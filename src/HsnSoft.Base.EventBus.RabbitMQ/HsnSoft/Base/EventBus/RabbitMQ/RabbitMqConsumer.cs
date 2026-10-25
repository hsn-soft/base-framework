using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Reads the queue of one event: up to <see cref="IntegrationEventInfo.FetchCount"/> messages at a time go to the dispatcher. A handled
/// message is acknowledged; a failed one is reported to the error client (<see cref="FailedEto"/>) and acknowledged, so it does not
/// block the queue; a timeout puts it back on the queue after a pause. It rebuilds its channel when the channel dies on its own, and on
/// disposal waits for the messages in flight.
/// </summary>
public sealed partial class RabbitMqConsumer(
    IRabbitMqPersistentConnection connection,
    IEventBusSubscriptionManager subscriptions,
    IEventDispatcher dispatcher,
    IFailedEventNotifier failedEventNotifier,
    EventBusOptions options,
    RabbitMqEventBusConfig config,
    ILogger logger,
    IntegrationEventInfo eventInfo) : IAsyncDisposable
{
    private readonly IRabbitMqPersistentConnection _connection = connection;
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly IEventDispatcher _dispatcher = dispatcher;
    private readonly IFailedEventNotifier _failedEventNotifier = failedEventNotifier;
    private readonly EventBusOptions _options = options;
    private readonly RabbitMqEventBusConfig _config = config;
    private readonly ILogger _logger = logger;
    private readonly IntegrationEventInfo _eventInfo = eventInfo;
    private readonly SemaphoreSlim _prefetch = new(eventInfo.FetchCount, eventInfo.FetchCount);
    private readonly SemaphoreSlim _recoveryGate = new(1, 1);
    private readonly Lock _channelLock = new();
    private readonly CancellationTokenSource _recoveryCts = new();
    private readonly CancellationTokenSource _handlerCts = new();
    private IChannel? _channel;
    private Task? _recoveryTask;
    private string _consumerTag = "no-active-consumer";
    private bool _disposed;

    public string QueueName { get; } = EventNameHelper.GetConsumerClientEventQueueName(options, eventInfo.EventName);

    /// <summary>Creates the queue (if needed), binds it to the event and starts consuming.</summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        IChannel channel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        _channel = channel;
        await StartConsumingAsync(channel, cancellationToken).ConfigureAwait(false);
        _recoveryTask = Task.Run(() => RecoveryWorkerAsync(_recoveryCts.Token), CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        LogTerminating(_logger, QueueName, _consumerTag);

        // no new work from here on: the broker stops delivering to this consumer, what it already delivered is finished below
        await CancelConsumerAsync().ConfigureAwait(false);

        await _recoveryCts.CancelAsync().ConfigureAwait(false);
        if (_recoveryTask is not null)
        {
            await Task.WhenAny(_recoveryTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        }

        // let the messages in flight finish; after the shutdown timeout their handlers are cancelled and the messages stay unacknowledged
        DateTime deadline = DateTime.UtcNow + _options.ShutdownTimeout;
        while (_prefetch.CurrentCount < _eventInfo.FetchCount && DateTime.UtcNow < deadline)
        {
            LogWaitingForMessages(_logger, QueueName, _eventInfo.FetchCount - _prefetch.CurrentCount);
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }

        await _handlerCts.CancelAsync().ConfigureAwait(false);

        IChannel? channel;
        lock (_channelLock)
        {
            channel = _channel;
            _channel = null;
        }

        if (channel is not null)
        {
            try
            {
                await channel.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogCloseFailed(_logger, QueueName, exception.Message);
            }

            channel.Dispose();
        }

        _recoveryCts.Dispose();
        _handlerCts.Dispose();
        LogTerminated(_logger, QueueName);
    }

    private async Task CancelConsumerAsync()
    {
        IChannel? channel = _channel;
        if (channel is null || channel.IsClosed)
        {
            return;
        }

        try
        {
            await channel.BasicCancelAsync(_consumerTag).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCancelFailed(_logger, QueueName, exception.Message);
        }
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        if (!_connection.IsConnected)
        {
            await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        // the client hands the consumer one delivery at a time unless told otherwise; the semaphore below only limits what it is given
        return await _connection.CreateModelAsync(_eventInfo.FetchCount, cancellationToken).ConfigureAwait(false);
    }

    private async Task StartConsumingAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(QueueName, _options.ExchangeName, _eventInfo.EventName, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.BasicQosAsync(0, _eventInfo.FetchCount, global: false, cancellationToken).ConfigureAwait(false);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += OnReceivedAsync;
        channel.ChannelShutdownAsync += OnChannelShutdownAsync;
        _consumerTag = await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken).ConfigureAwait(false);
        LogSubscribed(_logger, QueueName, channel.ChannelNumber);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_disposed)
        {
            // not taken: it goes back to the queue when the channel closes
            return;
        }

        await _prefetch.WaitAsync().ConfigureAwait(false);
        string eventName = "unknown";
        string message = string.Empty;
        try
        {
            eventName = ResolveEventName(args);
            message = Encoding.UTF8.GetString(args.Body.Span);

            // what the consumer itself says about this message (it failed, it was put back) belongs to its chain too
            using IDisposable? scope = _logger.BeginScope(MessageScope(eventName, args.Body.Span));
            if (_options.LogMessageBody)
            {
                LogReceivedWithBody(_logger, QueueName, message);
            }
            else
            {
                LogReceived(_logger, QueueName, eventName, args.Body.Length);
            }

            try
            {
                await ProcessEventAsync(eventName, args.Body.ToArray()).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_handlerCts.IsCancellationRequested)
            {
                // the service is stopping and the handler ran out of time: the message stays unacknowledged and is delivered again later
                return;
            }
            catch (TimeoutException exception)
            {
                LogTimeout(_logger, QueueName, exception.Message);
                await RequeueAfterDelayAsync(args.DeliveryTag).ConfigureAwait(false);
                return;
            }
            catch (Exception exception)
            {
                await ReportFailureAsync(args.DeliveryTag, eventName, message, exception).ConfigureAwait(false);
                return;
            }

            await AcknowledgeAsync(args.DeliveryTag).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // a step of the framework itself failed before the handler (reading the message, its logs): the message is reported as a failed one and taken off the queue, so it does not stay unacknowledged and the consumer goes on
            await ReportFailureAsync(args.DeliveryTag, eventName, message, exception).ConfigureAwait(false);
        }
        finally
        {
            _prefetch.Release();
        }
    }

    private static Dictionary<string, object?> MessageScope(string eventName, ReadOnlySpan<byte> body)
    {
        Dictionary<string, object?> scope = new() { ["EventName"] = eventName };
        try
        {
            ParentMessageEnvelope parent = EventSerializer.DeserializeParent(body);
            scope[LogFields.CorrelationId] = parent.CorrelationId;
            scope["MessageId"] = parent.MessageId;
        }
        catch (JsonException)
        {
            // a message that cannot be read has no chain to say; handling it reports that
        }

        return scope;
    }

    private async Task ProcessEventAsync(string eventName, byte[] body)
    {
        IntegrationEventInfo? info = _subscriptions.GetEventInfoByName(eventName);
        if (info is null)
        {
            LogNoSubscription(_logger, QueueName, eventName);
            return;
        }

        object envelope = EventSerializer.DeserializeEnvelope(body, info.EventType);
        await _dispatcher.DispatchAsync(eventName, info.EventType, envelope, _handlerCts.Token).ConfigureAwait(false);
    }

    /// <summary>The message could not be handled: tell the error client, then take it off the queue. When even that fails, try again later.</summary>
    private async Task ReportFailureAsync(ulong deliveryTag, string eventName, string message, Exception failure)
    {
        // a handling that failed logged its Error already (the ConsumeEnd, CONSUME_EVENT_ERROR): what follows is only the consequence, said below
        if (!failure.Data.Contains(EventDispatcher.ErrorLoggedKey))
        {
            Dictionary<string, object?> fields = [];
            ExceptionLogFields.AddTo(fields, failure);
            using (_logger.BeginScope(fields))
            {
                LogFailed(_logger, failure, QueueName, failure.Message);
            }
        }

        try
        {
            if (eventName == EventNames.For(_options, typeof(FailedEto)))
            {
                // a failed FailedEto must not be reported again, that would loop forever
                LogFailedEventLoop(_logger, QueueName, eventName, _options.LogMessageBody ? message : "(body not logged)", failure.Message);
            }
            else
            {
                await _failedEventNotifier.NotifyAsync(failure.Message, eventName, message, _handlerCts.Token).ConfigureAwait(false);
                LogMovedToErrorQueue(_logger, QueueName, EventNameHelper.GetErrorEventQueueName(_options, EventNames.For(_options, typeof(FailedEto))));
            }

            await AcknowledgeChannelAsync(deliveryTag).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RequeueAfterDelayAsync(deliveryTag).ConfigureAwait(false);
        }
    }

    private async Task AcknowledgeAsync(ulong deliveryTag)
    {
        try
        {
            await AcknowledgeChannelAsync(deliveryTag).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // handled, but the broker did not hear about it: it delivers the message again
            LogAckFailed(_logger, QueueName, exception.Message);
        }
    }

    private Task AcknowledgeChannelAsync(ulong deliveryTag)
        => EnsureChannelOrThrow().BasicAckAsync(deliveryTag, multiple: false).AsTask();

    private async Task RequeueAfterDelayAsync(ulong deliveryTag)
    {
        LogRequeue(_logger, QueueName, _config.RequeueDelay.TotalSeconds);
        await Task.Delay(_config.RequeueDelay).ConfigureAwait(false);
        try
        {
            await EnsureChannelOrThrow().BasicNackAsync(deliveryTag, multiple: false, requeue: true).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRequeueFailed(_logger, QueueName, exception.Message);
        }
    }

    private IChannel EnsureChannelOrThrow()
    {
        IChannel? channel = _channel;
        if (channel is null || channel.IsClosed)
        {
            throw new InvalidOperationException("Consumer channel is not available.");
        }

        return channel;
    }

    private Task OnChannelShutdownAsync(object sender, ShutdownEventArgs args)
    {
        LogChannelShutdown(_logger, QueueName, args.ReplyText ?? "no reason given");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The client's automatic recovery brings back channels and consumers after a lost connection, but not a channel that dies on its
    /// own while the connection is fine (a protocol error, a cancelled consumer). This loop notices that and rebuilds the channel.
    /// </summary>
    private async Task RecoveryWorkerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // while the connection is down the client is recovering it; touching the channel now would race that
                if (_connection.IsConnected && IsChannelDead() && await _recoveryGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        // checked again under the gate: another pass may have fixed it already
                        if (IsChannelDead())
                        {
                            await RebuildChannelAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        _recoveryGate.Release();
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogRecoveryFailed(_logger, QueueName, exception.Message);
            }

            try
            {
                await Task.Delay(_config.RecoveryProbeInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // the consumer is being disposed
            }
        }
    }

    private bool IsChannelDead()
    {
        IChannel? channel = _channel;
        return !_disposed && (channel is null || channel.IsClosed);
    }

    private async Task RebuildChannelAsync(CancellationToken cancellationToken)
    {
        IChannel newChannel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        IChannel? old;
        lock (_channelLock)
        {
            old = _channel;
            _channel = newChannel;
        }

        old?.Dispose();
        await StartConsumingAsync(newChannel, cancellationToken).ConfigureAwait(false);
        LogChannelRebuilt(_logger, QueueName);
    }

    // a message that came through the exchange has its event name as routing key; one put straight on a queue only has "{consumer}_{event}"
    private static string ResolveEventName(BasicDeliverEventArgs args)
        => string.IsNullOrWhiteSpace(args.Exchange) ? args.RoutingKey.Split('_').Last() : args.RoutingKey;

    [LoggerMessage(EventId = 4001, EventName = "RABBITMQ_CONSUMER_SUBSCRIBED", Level = LogLevel.Information, Message = "{Queue} => channel {Channel}: SUBSCRIBED")]
    private static partial void LogSubscribed(ILogger logger, string queue, int channel);

    [LoggerMessage(EventId = 4002, EventName = "RABBITMQ_CONSUMER_TERMINATING", Level = LogLevel.Information, Message = "{Queue} => consumer {ConsumerTag}: TERMINATING")]
    private static partial void LogTerminating(ILogger logger, string queue, string consumerTag);

    [LoggerMessage(EventId = 4003, EventName = "RABBITMQ_CONSUMER_TERMINATED", Level = LogLevel.Information, Message = "{Queue}: TERMINATED")]
    private static partial void LogTerminated(ILogger logger, string queue);

    [LoggerMessage(EventId = 4004, EventName = "RABBITMQ_CONSUMER_WAITING_FOR_MESSAGES", Level = LogLevel.Debug, Message = "{Queue}: waiting for {Count} message(s) in flight")]
    private static partial void LogWaitingForMessages(ILogger logger, string queue, int count);

    [LoggerMessage(EventId = 4005, EventName = "RABBITMQ_CONSUMER_CANCEL_FAILED", Level = LogLevel.Warning, Message = "{Queue}: the consumer could not be cancelled, the channel is closed instead: {Error}")]
    private static partial void LogCancelFailed(ILogger logger, string queue, string error);

    [LoggerMessage(EventId = 4006, EventName = "RABBITMQ_CONSUMER_CLOSE_FAILED", Level = LogLevel.Error, Message = "{Queue}: the channel could not be closed cleanly: {Error}")]
    private static partial void LogCloseFailed(ILogger logger, string queue, string error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Queue}: received {EventName} ({Size} bytes)")]
    private static partial void LogReceived(ILogger logger, string queue, string eventName, int size);

    [LoggerMessage(EventId = 4007, EventName = "RABBITMQ_CONSUMER_RECEIVED_WITH_BODY", Level = LogLevel.Debug, Message = "{Queue}: received {Message}")]
    private static partial void LogReceivedWithBody(ILogger logger, string queue, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Queue}: message without a subscription for event {EventName}")]
    private static partial void LogNoSubscription(ILogger logger, string queue, string eventName);

    [LoggerMessage(EventId = 4008, EventName = "RABBITMQ_CONSUMER_TIMEOUT", Level = LogLevel.Warning, Message = "{Queue}: TIMEOUT, the message goes back on the queue ({Error})")]
    private static partial void LogTimeout(ILogger logger, string queue, string error);

    [LoggerMessage(EventId = 4009, EventName = "RABBITMQ_CONSUMER_FAILED", Level = LogLevel.Error, Message = "{Queue}: ERROR ({Error})")]
    private static partial void LogFailed(ILogger logger, Exception exception, string queue, string error);

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Queue}: a failed {EventName} event could not be handled and is dropped: {Message} ({Error})")]
    private static partial void LogFailedEventLoop(ILogger logger, string queue, string eventName, string message, string error);

    [LoggerMessage(EventId = 4010, EventName = "RABBITMQ_CONSUMER_MOVED_TO_ERROR_QUEUE", Level = LogLevel.Warning, Message = "{Queue}: message reported to {ErrorQueue} and taken off the queue (moved to the error queue)")]
    private static partial void LogMovedToErrorQueue(ILogger logger, string queue, string errorQueue);

    [LoggerMessage(EventId = 4011, EventName = "RABBITMQ_CONSUMER_ACK_FAILED", Level = LogLevel.Error, Message = "{Queue}: the message was handled but could not be acknowledged: {Error}")]
    private static partial void LogAckFailed(ILogger logger, string queue, string error);

    [LoggerMessage(EventId = 4012, EventName = "RABBITMQ_CONSUMER_REQUEUE", Level = LogLevel.Warning, Message = "{Queue}: requeue after {DelaySeconds} s")]
    private static partial void LogRequeue(ILogger logger, string queue, double delaySeconds);

    [LoggerMessage(EventId = 4013, EventName = "RABBITMQ_CONSUMER_REQUEUE_FAILED", Level = LogLevel.Error, Message = "{Queue}: the message could not be put back on the queue: {Error}")]
    private static partial void LogRequeueFailed(ILogger logger, string queue, string error);

    [LoggerMessage(EventId = 4014, EventName = "RABBITMQ_CONSUMER_CHANNEL_SHUTDOWN", Level = LogLevel.Warning, Message = "{Queue}: channel shut down ({Reason})")]
    private static partial void LogChannelShutdown(ILogger logger, string queue, string reason);

    [LoggerMessage(EventId = 4015, EventName = "RABBITMQ_CONSUMER_RECOVERY_FAILED", Level = LogLevel.Error, Message = "{Queue}: the channel could not be recovered, trying again: {Error}")]
    private static partial void LogRecoveryFailed(ILogger logger, string queue, string error);

    [LoggerMessage(EventId = 4016, EventName = "RABBITMQ_CONSUMER_CHANNEL_REBUILT", Level = LogLevel.Information, Message = "{Queue}: the channel died and was rebuilt")]
    private static partial void LogChannelRebuilt(ILogger logger, string queue);
}
