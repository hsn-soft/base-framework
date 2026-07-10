using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
    private readonly IEventDispatcher _dispatcher;
    private readonly IFailedEventNotifier _failedEventNotifier;

    private readonly SemaphoreSlim _consumerPrefetchSemaphore;
    private IChannel _consumerChannel;
    private readonly Lock _channelLock = new();

    // Single-flight guard for RecoveryWorkerAsync's detect->recreate->resubscribe sequence — without it,
    // two racing passes of the 5s loop (or this loop racing RabbitMqPersistentConnection's own automatic
    // recovery) could both decide the channel is dead and double-subscribe the same queue.
    private readonly SemaphoreSlim _recoveryGate = new(1, 1);

    private bool _disposed;
    private string _currentConsumerTag = "no-active-consumer";
    private string _consumerQueueName = string.Empty;
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
        IEventDispatcher dispatcher,
        IFailedEventNotifier failedEventNotifier,
        string consumerEventName)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory), "MessageBroker ServiceScopeFactory is null");
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig ?? throw new ArgumentNullException(nameof(rabbitMqEventBusConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _failedEventNotifier = failedEventNotifier ?? throw new ArgumentNullException(nameof(failedEventNotifier));
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
        _recoveryGate?.Dispose();

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

            string fetcherId = Task.CurrentId?.ToString() ?? Guid.CreateVersion7().ToString("N");
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
                        await _failedEventNotifier.NotifyAsync(ex.Message, eventName, message);
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

            // Boundary rule lives in IEventDispatcher: a handler that can't be resolved via DI is a
            // pre-handler/framework-level failure and throws (propagates to trigger FailedEto below);
            // a handler that resolves and runs but throws during its own execution is logged only and
            // never rethrown — that failure is the microservice's own responsibility.
            await _dispatcher.DispatchAsync(eventName, eventInfo.EventType, @event);
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

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected) await _persistentConnection.TryConnectAsync();

        // ConsumerDispatchConcurrency mirrors FetchCount so RabbitMQ.Client actually dispatches up to
        // FetchCount deliveries concurrently on this channel — without it, the client's own internal
        // dispatcher (default concurrency=1) serializes every delivery regardless of prefetch/FetchCount,
        // leaving _consumerPrefetchSemaphore below with nothing to throttle.
        var channel = await _persistentConnection.CreateModelAsync(_consumerEventInfo.FetchCount);
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
                // AutomaticRecoveryEnabled (RabbitMqPersistentConnection) owns reconnecting the connection
                // itself, retrying forever on its own schedule — while disconnected there is nothing safe
                // for this loop to do except wait, since touching the channel here would race the
                // library's own in-flight recovery. We deliberately do NOT call TryConnectAsync() here.
                if (_persistentConnection.IsConnected)
                {
                    var channel = _consumerChannel;
                    if (channel == null || channel.IsClosed)
                    {
                        // Connection is healthy but this specific channel died independently (e.g. a
                        // broker-initiated basic.cancel from a deleted/moved queue, or a channel-level
                        // protocol error) — the one gap RabbitMQ.Client's automatic recovery doesn't cover,
                        // since it only recovers channels/consumers as a subroutine of *connection*
                        // recovery. Rebuild immediately, single-flight guarded.
                        if (await _recoveryGate.WaitAsync(0, ct))
                        {
                            try
                            {
                                // Re-check under the gate: another pass may have already fixed it.
                                channel = _consumerChannel;
                                if (channel == null || channel.IsClosed)
                                {
                                    _logger.LogWarning("{BrokerName} | Recovery: Channel is null/closed while connection is healthy. Recreating...", "RabbitMQ");
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
                            finally
                            {
                                _recoveryGate.Release();
                            }
                        }
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