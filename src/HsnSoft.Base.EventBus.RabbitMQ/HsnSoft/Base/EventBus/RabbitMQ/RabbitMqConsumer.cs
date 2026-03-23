using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public sealed class RabbitMqConsumer : IDisposable
{
    private const int MaxWaitDisposeTimeMs = 30000;

    // Recovery settings
    private static readonly TimeSpan s_recoveryProbeInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_subscribeRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;

    private readonly SemaphoreSlim _consumerPrefetchSemaphore;
    private IChannel _consumerChannel;
    private readonly Lock _channelLock = new();

    private bool _disposed;
    private string _currentConsumerTag = "no-active-consumer";
    private string _consumerQueueName = string.Empty;
    private string _consumerErrorQueueName = string.Empty;
    private readonly string _consumerEventName;
    private readonly IntegrationEventInfo _consumerEventInfo;

    // Background recovery worker
    private readonly CancellationTokenSource _recoveryCts = new();
    private readonly Task _recoveryTask;

    public RabbitMqConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IRabbitMqPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        RabbitMqEventBusConfig rabbitMqEventBusConfig,
        IEventBusLogger logger,
        string consumerEventName)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory), "MessageBroker ServiceScopeFactory is null");
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig ?? throw new ArgumentNullException(nameof(rabbitMqEventBusConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consumerEventName = consumerEventName ?? throw new ArgumentNullException(nameof(consumerEventName));

        _consumerEventInfo = _subscriptionsManager.GetEventInfoByName(_consumerEventName) ?? throw new InvalidOperationException($"No event info for {_consumerEventName}");

        _consumerPrefetchSemaphore = new SemaphoreSlim(_consumerEventInfo.FetchCount > 0 ? _consumerEventInfo.FetchCount : 1);

        // channel first creation
        _consumerChannel = CreateConsumerChannelAsync().GetAwaiter().GetResult()
                           ?? throw new InvalidOperationException("Cannot create consumer channel on startup.");

        // channel recovery worker start
        _recoveryTask = Task.Run(() => RecoveryWorkerAsync(_recoveryCts.Token));
    }

    public async Task StartBasicConsume()
    {
        if (_disposed) return;

        var channel = EnsureChannelOrThrow();
        _consumerQueueName = EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, _consumerEventName);

        // Prefetch/QoS
        await channel.BasicQosAsync(0, _consumerEventInfo?.FetchCount ?? 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += ConsumerReceivedAsync;

        // catch channel shutdown or exception
        channel.CallbackExceptionAsync += OnCallbackExceptionAsync;
        channel.ChannelShutdownAsync += OnModelShutdownAsync;

        // Subscribe
        _currentConsumerTag = await channel.BasicConsumeAsync(
            queue: _consumerQueueName,
            autoAck: false,
            consumer: consumer);

        string consumerChannelNumber = channel.ChannelNumber.ToString();
        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ]: {OperationStatus}",
            "RabbitMQ", _consumerQueueName, consumerChannelNumber, "SUBSCRIBED");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        string channelNo = _consumerChannel?.ChannelNumber.ToString() ?? "0";

        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: {OperationStatus}",
            "RabbitMQ", _consumerQueueName, channelNo, _currentConsumerTag, "TERMINATING");

        // close channel recovery worker
        try
        {
            _recoveryCts.Cancel();
            try
            {
                _recoveryTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                /* ignore */
            }
        }
        catch
        {
            /* ignore */
        }

        // wait continuous message processes
        ushort fetchCount = _consumerEventInfo?.FetchCount ?? 1;
        int waitedMs = 0;
        while (waitedMs < MaxWaitDisposeTimeMs && _consumerPrefetchSemaphore.CurrentCount < fetchCount)
        {
            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: Consumer Fetcher [ {Done}/{All} ] wait processing...",
                "RabbitMQ", _consumerQueueName, channelNo, _currentConsumerTag, _consumerPrefetchSemaphore.CurrentCount, fetchCount);
            Thread.Sleep(1000);
            waitedMs += 1000;
        }

        _consumerPrefetchSemaphore?.Dispose();

        try
        {
            lock (_channelLock)
            {
                _consumerChannel?.CloseAsync().GetAwaiter().GetResult();
                _consumerChannel?.Dispose();
                _consumerChannel = null;
            }
        }
        catch
        {
            /* ignore */
        }

        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: {OperationStatus}",
            "RabbitMQ", _consumerQueueName, channelNo, _currentConsumerTag, "TERMINATED");
    }

    private async Task ConsumerReceivedAsync([CanBeNull] object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed)
        {
            // don't use semaphore count until disposed function semaphore count check
            while (_consumerPrefetchSemaphore.CurrentCount < (_consumerEventInfo?.FetchCount ?? 1))
                Thread.Sleep(200);
            return;
        }

        await _consumerPrefetchSemaphore.WaitAsync();

        try
        {
            _currentConsumerTag = (sender as AsyncEventingBasicConsumer)?.ConsumerTags.FirstOrDefault() ?? "no-active-consumer";
            string consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";

            string eventName = ResolveEventName(eventArgs);
            string message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            string fetcherId = Task.CurrentId?.ToString() ?? Guid.NewGuid().ToString("N");
            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus}",
                "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "STARTED");

            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: ReceivedMessageEnvelope {ReceivedMessageEnvelope}",
                "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, message);

            var stopWatch = Stopwatch.StartNew();
            try
            {
                await ProcessEvent(eventName, message);

                // ACK — async/await (block yok)
                var ch = EnsureChannelOrThrow();
                await ch.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);

                stopWatch.Stop();
                _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus} [ {ConsumeHandleWorkingTime}sn ]",
                    "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "COMPLETED", stopWatch.Elapsed.TotalSeconds.ToString("0.###"));
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: TIMEOUT_RETRY ({Err})",
                    "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, tex.Message);

                await TryEnqueueMessageAgainAsync(eventArgs, fetcherId);
            }
            catch (Exception ex)
            {
                _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: ERROR ( {ConsumeError} ) | {Time}",
                    "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));

                try
                {
                    if (eventName.Equals(EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, nameof(FailedEto))))
                    {
                        // FATAL ERROR: event error handling loop
                        _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: FailedEvent {FailedEvent} Handling error, {Error}", "RabbitMQ",
                            _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, message, ex.Message);
                    }
                    else
                    {
                        await ConsumeErrorPublishAsync(ex.Message, eventName, message);
                        _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Message moved to ErrorHandlerQueue", "RabbitMQ",
                            _consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId);
                    }

                    // remove crashed message from old queue
                    var ch = EnsureChannelOrThrow();
                    await ch.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                }
                catch
                {
                    // re-try consume
                    await TryEnqueueMessageAgainAsync(eventArgs, fetcherId);
                }
            }
        }
        finally
        {
            _consumerPrefetchSemaphore.Release();
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            var eventInfo = _subscriptionsManager.GetEventInfoByName(eventName);

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(eventInfo!.EventType);
            object @event = System.Text.Json.JsonSerializer.Deserialize(message, constructedClass);
            //Guid messageId = ((dynamic)@event)?.MessageId;

            var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
            // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscription can be for one Event
            foreach (var subscription in subscriptions)
            {
                using var scope = _serviceScopeFactory.CreateScope(); // because handler type scoped service
                object handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    _logger.LogWarning("{BrokerName} | CONSUMER {ClientInfo} EVENT [ {EventName} ] => {OperationStatus} for event", "RabbitMQ",
                        _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, "NO_HANDLER");
                    continue;
                }

                var watch = Stopwatch.StartNew();
                var handleStartTime = DateTimeOffset.UtcNow;

                try
                {
                    var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventInfo.EventType!);
                    await Task.Yield();

                    var method = eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEventMessage>.HandleAsync));
                    await ((Task)method!.Invoke(handler, [@event]))!;

                    watch.Stop();
                    _logger.EventBusInfoLog(new ConsumeMessageLogModel(
                        LogId: Guid.NewGuid().ToString(),
                        CorrelationId: ((dynamic)@event)?.CorrelationId,
                        Facility: nameof(EventBusLogFacility.CONSUME_EVENT_SUCCESS),
                        Producer: ((dynamic)@event)?.Producer,
                        ConsumeDateTimeUtc: handleStartTime,
                        MessageLog: new MessageLogDetail(
                            EventType: eventName,
                            HopLevel: ((dynamic)@event)?.HopLevel,
                            ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                            MessageId: ((dynamic)@event)?.MessageId,
                            MessageTime: ((dynamic)@event)?.MessageTime,
                            Message: ((dynamic)@event)?.Message,
                            UserId: ((dynamic)@event)?.UserId,
                            UserRoles: ((dynamic)@event)?.UserRoles,
                            ClientLat: ((dynamic)@event)?.ClientLat,
                            ClientLong: ((dynamic)@event)?.ClientLong,
                            ClientChannel: ((dynamic)@event)?.ClientChannel,
                            ClientVersion: ((dynamic)@event)?.ClientVersion
                        ),
                        ConsumeDetails: "Message handling successfully completed",
                        ConsumeHandleWorkingTimeMs: watch.ElapsedMilliseconds
                    ));
                }
                catch (Exception ex)
                {
                    watch.Stop();
                    _logger.EventBusErrorLog(new ConsumeMessageLogModel(
                        LogId: Guid.NewGuid().ToString(),
                        CorrelationId: ((dynamic)@event)?.CorrelationId,
                        Facility: nameof(EventBusLogFacility.CONSUME_EVENT_ERROR),
                        Producer: ((dynamic)@event)?.Producer,
                        ConsumeDateTimeUtc: handleStartTime,
                        MessageLog: new MessageLogDetail(
                            EventType: eventName,
                            HopLevel: ((dynamic)@event)?.HopLevel,
                            ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                            MessageId: ((dynamic)@event)?.MessageId,
                            MessageTime: ((dynamic)@event)?.MessageTime,
                            Message: ((dynamic)@event)?.Message,
                            UserId: ((dynamic)@event)?.UserId,
                            UserRoles: ((dynamic)@event)?.UserRoles,
                            ClientLat: ((dynamic)@event)?.ClientLat,
                            ClientLong: ((dynamic)@event)?.ClientLong,
                            ClientChannel: ((dynamic)@event)?.ClientChannel,
                            ClientVersion: ((dynamic)@event)?.ClientVersion
                        ),
                        ConsumeDetails: $"Handle Error: {ex.Message}",
                        ConsumeHandleWorkingTimeMs: watch.ElapsedMilliseconds
                    ));

                    throw;
                }
            }
        }
        else
        {
            _logger.LogWarning("{BrokerName} | CONSUMER {ClientInfo} EVENT [ {EventName} ] => {OperationStatus} for event", "RabbitMQ",
                _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, "NO_SUBSCRIPTION");
        }
    }

    private async Task TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs, string taskId)
    {
        string consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";

        _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Requeue with {Delay}s delay...",
            "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0", $"{s_subscribeRetryDelay.TotalSeconds:n1}");

        await Task.Delay(s_subscribeRetryDelay);

        try
        {
            var ch = EnsureChannelOrThrow();
            await ch.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);

            _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Message requeued",
                "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0");
        }
        catch (Exception ex)
        {
            _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Could not enqueue message again: {Error}",
                "RabbitMQ", _consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0", ex.Message);
        }
    }

    private async Task ConsumeErrorPublishAsync([NotNull] string errorMessage, [NotNull] string failedEventName, [NotNull] string failedMessageContent)
    {
        if (!_persistentConnection.IsConnected) await _persistentConnection.TryConnectAsync();
        if (!_persistentConnection.IsConnected) throw new ConnectFailureException("", new Exception("Connection fail"));

        ParentMessageEnvelope failedEnvelopeInfo = null;
        Type failedEventEnvelopeMessageType = null;
        IIntegrationEventMessage failedMessageObject = null;
        try
        {
            dynamic failedEnvelope = JsonConvert.DeserializeObject<dynamic>(failedMessageContent);
            failedEnvelopeInfo = ((JObject)failedEnvelope)?.ToObject<ParentMessageEnvelope>();

            failedEventEnvelopeMessageType = _subscriptionsManager.GetEventInfoByName(failedEventName)?.EventType;

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(failedEventEnvelopeMessageType!);
            object failedEventEnvelope = System.Text.Json.JsonSerializer.Deserialize(failedMessageContent, constructedClass);

            failedMessageObject = ((dynamic)failedEventEnvelope)?.Message;
        }
        catch (Exception e)
        {
            errorMessage += ". FailedMessageContent convert operation error: " + e.Message;
        }

        var produceTime = DateTime.UtcNow;
        var @event = new MessageEnvelope<FailedEto>
        {
            ParentMessageId = failedEnvelopeInfo?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = produceTime,
            Message = new FailedEto(
                FailedReason: errorMessage,
                FailedMessageEnvelopeTime: failedEnvelopeInfo?.MessageTime.ToUniversalTime(),
                FailedMessageObject: failedMessageObject,
                FailedMessageTypeName: failedEventEnvelopeMessageType?.Name
            ),
            Producer = _rabbitMqEventBusConfig.ConsumerClientInfo,
            CorrelationId = failedEnvelopeInfo?.CorrelationId,
            UserId = failedEnvelopeInfo?.UserId,
            UserRoles = failedEnvelopeInfo?.UserRoles,
            ClientLat = failedEnvelopeInfo?.ClientLat,
            ClientLong = failedEnvelopeInfo?.ClientLong,
            ClientChannel = failedEnvelopeInfo?.ClientChannel,
            ClientVersion = failedEnvelopeInfo?.ClientVersion,
            HopLevel = failedEnvelopeInfo != null ? (ushort)(failedEnvelopeInfo.HopLevel + 1) : (ushort)1,
            ReQueuedCount = failedEnvelopeInfo?.ReQueuedCount ?? 0
        };

        string eventName = EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, @event.Message.GetType().Name);
        _consumerErrorQueueName = $"{_rabbitMqEventBusConfig.ErrorClientInfo}_{eventName}";

        _logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] STARTED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogError("{BrokerName} | Could not publish failed event message : {Event} after {Timeout}s ({ExceptionMessage})",
                    "RabbitMQ", failedMessageContent, $"{time.TotalSeconds:n1}", ex.Message);

                // Persistent Log
                _logger.EventBusErrorLog(new ProduceMessageLogModel(
                    LogId: Guid.NewGuid().ToString(),
                    CorrelationId: @event.CorrelationId,
                    Facility: nameof(EventBusLogFacility.PRODUCE_EVENT_ERROR),
                    ProduceDateTimeUtc: produceTime,
                    MessageLog: new MessageLogDetail(
                        EventType: eventName,
                        HopLevel: @event.HopLevel,
                        ParentMessageId: @event.ParentMessageId,
                        MessageId: @event.MessageId,
                        MessageTime: @event.MessageTime,
                        Message: @event.Message,
                        UserId: @event.UserId,
                        UserRoles: @event.UserRoles,
                        ClientLat: @event.ClientLat,
                        ClientLong: @event.ClientLong,
                        ClientChannel: @event.ClientChannel,
                        ClientVersion: @event.ClientVersion
                    ),
                    ProduceDetails: $"Message publish error: {ex.Message}"));
            });

        byte[] body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        await policy.ExecuteAsync(async () =>
        {
            await using var publisherChannel = await _persistentConnection.CreateModelAsync()!;

            await publisherChannel.QueueDeclareAsync(
                queue: _consumerErrorQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await publisherChannel.BasicPublishAsync(
                exchange: "",
                routingKey: _consumerErrorQueueName,
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        });

        _logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());
    }

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected) await _persistentConnection.TryConnectAsync();

        var channel = await _persistentConnection.CreateModelAsync();
        await channel.ExchangeDeclareAsync(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");
        return channel;
    }

    private IChannel EnsureChannelOrThrow()
    {
        var ch = _consumerChannel;
        if (ch == null || ch.IsClosed)
            throw new InvalidOperationException("Consumer channel is not available.");
        return ch;
    }

    private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs args)
    {
        _logger.LogError("{BrokerName} | Channel CallbackException: {Error}", "RabbitMQ", args?.Exception.Message ?? "UNKNOWN");
        return Task.CompletedTask;
    }

    private Task OnModelShutdownAsync(object sender, ShutdownEventArgs args)
    {
        _logger.LogWarning("{BrokerName} | Channel Shutdown: {ReplyCode} {ReplyText}", "RabbitMQ", args.ReplyCode, args.ReplyText);
        return Task.CompletedTask;
    }

    private async Task RecoveryWorkerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_persistentConnection.IsConnected) await _persistentConnection.TryConnectAsync();
                if (!_persistentConnection.IsConnected) throw new ConnectFailureException("", new Exception("Connection fail"));

                var channel = _consumerChannel;
                if (channel == null || channel.IsClosed)
                {
                    _logger.LogWarning("{BrokerName} | Recovery: Channel is null/closed. Recreating...", "RabbitMQ");
                    var newChannel = await CreateConsumerChannelAsync();
                    if (newChannel != null)
                    {
                        lock (_channelLock)
                        {
                            _consumerChannel?.Dispose();
                            _consumerChannel = newChannel;
                        }

                        await StartBasicConsume();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{BrokerName} | Recovery loop error: {Error}", "RabbitMQ", ex.Message);
            }

            try
            {
                await Task.Delay(s_recoveryProbeInterval, ct);
            }
            catch (TaskCanceledException)
            {
                /* shutting down */
            }
        }
    }

    private string ResolveEventName(BasicDeliverEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(eventArgs.Exchange)) // direct queue
            return eventArgs.RoutingKey.Split("_").Last();

        return eventArgs.RoutingKey;
    }
}