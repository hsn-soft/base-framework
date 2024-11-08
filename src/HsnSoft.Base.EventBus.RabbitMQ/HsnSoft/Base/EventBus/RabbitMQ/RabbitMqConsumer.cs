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
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public sealed class RabbitMqConsumer : IDisposable
{
    private const int MaxWaitDisposeTime = 30000;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;

    private static readonly object ChannelAckResourceLock = new();
    private readonly SemaphoreSlim _consumerPrefetchSemaphore;
    private readonly IModel _consumerChannel;
    private bool _disposed;
    private string _currentConsumerTag = "no-active-consumer";
    private string consumerQueueName = string.Empty;
    private string consumerErrorQueueName = string.Empty;

    public RabbitMqConsumer(IServiceScopeFactory serviceScopeFactory,
        IRabbitMqPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        RabbitMqEventBusConfig rabbitMqEventBusConfig,
        IEventBusLogger logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory), "MessageBroker ServiceScopeFactory is null");
        _persistentConnection = persistentConnection;
        _subscriptionsManager = subscriptionsManager;
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig;
        _logger = logger;

        _consumerChannel = CreateConsumerChannel();
        _consumerPrefetchSemaphore = new SemaphoreSlim(_rabbitMqEventBusConfig.ConsumerMaxFetchCount);
    }

    public void StartBasicConsume(string eventName)
    {
        if (_consumerChannel == null)
        {
            _logger.LogError("{BrokerName} | StartBasicConsume can't call on _consumerChannel == null", "RabbitMQ");
            return;
        }

        lock (ChannelAckResourceLock)
        {
            consumerQueueName = EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, eventName);
            _consumerChannel?.BasicQos(0, _rabbitMqEventBusConfig.ConsumerMaxFetchCount, false);
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += ConsumerReceived;
            _consumerChannel?.BasicConsume(queue: consumerQueueName, autoAck: false, consumer: consumer);
        }

        var consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";
        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ]: {OperationStatus}", "RabbitMQ",
            consumerQueueName, consumerChannelNumber, "SUBSCRIBED");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";

        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: {OperationStatus}", "RabbitMQ",
            consumerQueueName, consumerChannelNumber, _currentConsumerTag, "TERMINATING");

        var waitCounter = 0;
        while (waitCounter * 1000 < MaxWaitDisposeTime && _consumerPrefetchSemaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerMaxFetchCount)
        {
            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: Consumer Fetcher [ {Done}/{All} ] wait processing...", "RabbitMQ",
                consumerQueueName, consumerChannelNumber, _currentConsumerTag, _consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
            Thread.Sleep(1000);
            waitCounter++;
        }

        if (waitCounter > 0)
        {
            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: Consumer Fetcher [ {Done}/{All} ] processed", "RabbitMQ",
                consumerQueueName, consumerChannelNumber, _currentConsumerTag, _consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
        }

        _consumerPrefetchSemaphore?.Dispose();
        _consumerChannel?.Close();
        _consumerChannel?.Dispose();

        _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ]: {OperationStatus}", "RabbitMQ",
            consumerQueueName, consumerChannelNumber, _currentConsumerTag, "TERMINATED");
    }

    private async Task ConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed)
        {
            // don't use semaphore count until disposed function semaphore count check
            while (_consumerPrefetchSemaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerMaxFetchCount)
            {
                Thread.Sleep(1000);
            }

            return;
        }

        await _consumerPrefetchSemaphore.WaitAsync();

        _currentConsumerTag = (sender as AsyncEventingBasicConsumer)?.ConsumerTags.FirstOrDefault();
        _currentConsumerTag = string.IsNullOrWhiteSpace(_currentConsumerTag) ? "no-active-consumer" : _currentConsumerTag;
        var consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";

        var eventName = eventArgs.RoutingKey;
        if (string.IsNullOrWhiteSpace(eventArgs.Exchange)) // No-Fanout-Exchange direct queue
        {
            eventName = eventArgs.RoutingKey.Split("_").Last();
        }

        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        {
            var fetcherId = Task.CurrentId?.ToString() ?? "0";
            _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus}", "RabbitMQ",
                consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "STARTED");

            try
            {
                _logger.LogDebug("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: ReceivedMessageEnvelope {ReceivedMessageEnvelope}", "RabbitMQ",
                    consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, message);

                var stopWatch = Stopwatch.StartNew();

                await ProcessEvent(eventName, message);

                stopWatch.Stop();
                var timespan = stopWatch.Elapsed;

                lock (ChannelAckResourceLock)
                {
                    _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }

                _logger.LogInformation("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus} [ {ConsumeHandleWorkingTime}sn ]", "RabbitMQ",
                    consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "COMPLETED", timespan.TotalSeconds.ToString("0.###"));
            }
            catch (TimeoutException timeProblem)
            {
                // re-try consume
                _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus} ( TimeProblem )", "RabbitMQ",
                    consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "TIMEOUT_RETRY", timeProblem.Message);

                TryEnqueueMessageAgainAsync(eventArgs, fetcherId);
            }
            catch (Exception ex)
            {
                _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: {OperationStatus} ( {ConsumeError} ) | {Time}", "RabbitMQ",
                    consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, "ERROR", ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                try
                {
                    if (eventName.Equals(EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, nameof(FailedEto))))
                    {
                        // FATAL ERROR: event error handling loop
                        _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: FailedEvent {FailedEvent} Handling error, {Error}", "RabbitMQ",
                            consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId, message, ex.Message);
                    }
                    else
                    {
                        ConsumeErrorPublish(ex.Message, eventName, message);
                        _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Message moved to ErrorHandlerQueue", "RabbitMQ",
                            consumerQueueName, consumerChannelNumber, _currentConsumerTag, fetcherId);
                    }

                    // remove from old queue
                    lock (ChannelAckResourceLock)
                    {
                        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception)
                {
                    // re-try consume
                    TryEnqueueMessageAgainAsync(eventArgs, fetcherId);
                }
            }
            finally
            {
                _consumerPrefetchSemaphore.Release();
            }
        });
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            var eventType = _subscriptionsManager.GetEventTypeByName(eventName);

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(eventType!);
            var @event = JsonSerializer.Deserialize(message, constructedClass);
            Guid messageId = ((dynamic)@event)?.MessageId;

            var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
            // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscription can be for one Event
            foreach (var subscription in subscriptions)
            {
                using var scope = _serviceScopeFactory.CreateScope(); // because handler type scoped service
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    _logger.LogWarning("{BrokerName} | CONSUMER {ClientInfo} EVENT [ {EventName} ] => {OperationStatus} for event", "RabbitMQ",
                        _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, "NO_HANDLER");
                    continue;
                }

                var watch = new Stopwatch();
                watch.Start();
                var handleStartTime = DateTimeOffset.UtcNow;
                try
                {
                    var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await Task.Yield();
                    await ((Task)eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEventMessage>.HandleAsync))?.Invoke(handler, new[] { @event }))!;

                    watch.Stop();
                    _logger.EventBusInfoLog(new ConsumeMessageLogModel(
                        LogId: Guid.NewGuid().ToString(),
                        CorrelationId: ((dynamic)@event)?.CorrelationId,
                        Facility: EventBusLogFacility.CONSUME_EVENT_SUCCESS.ToString(),
                        Producer: ((dynamic)@event)?.Producer,
                        ConsumeDateTimeUtc: handleStartTime,
                        MessageLog: new MessageLogDetail(
                            EventType: eventName,
                            HopLevel: ((dynamic)@event)?.HopLevel,
                            ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                            MessageId: ((dynamic)@event)?.MessageId,
                            MessageTime: ((dynamic)@event)?.MessageTime,
                            Message: ((dynamic)@event)?.Message,
                            UserInfo: new EventUserDetail(
                                UserId: ((dynamic)@event)?.UserId,
                                Role: ((dynamic)@event)?.UserRoleUniqueName
                            )),
                        ConsumeDetails: "Message handling successfully completed",
                        ConsumeHandleWorkingTime: $"{watch.ElapsedMilliseconds:0.####}ms"));
                }
                catch (Exception ex)
                {
                    watch.Stop();
                    _logger.EventBusErrorLog(new ConsumeMessageLogModel(
                        LogId: Guid.NewGuid().ToString(),
                        CorrelationId: ((dynamic)@event)?.CorrelationId,
                        Facility: EventBusLogFacility.CONSUME_EVENT_ERROR.ToString(),
                        Producer: ((dynamic)@event)?.Producer,
                        ConsumeDateTimeUtc: handleStartTime,
                        MessageLog: new MessageLogDetail(
                            EventType: eventName,
                            HopLevel: ((dynamic)@event)?.HopLevel,
                            ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                            MessageId: ((dynamic)@event)?.MessageId,
                            MessageTime: ((dynamic)@event)?.MessageTime,
                            Message: ((dynamic)@event)?.Message,
                            UserInfo: new EventUserDetail(
                                UserId: ((dynamic)@event)?.UserId,
                                Role: ((dynamic)@event)?.UserRoleUniqueName
                            )),
                        ConsumeDetails: $"Handle Error: {ex.Message}",
                        ConsumeHandleWorkingTime: $"{watch.ElapsedMilliseconds:0.####}ms"));

                    throw ex;
                }
            }
        }
        else
        {
            _logger.LogWarning("{BrokerName} | CONSUMER {ClientInfo} EVENT [ {EventName} ] => {OperationStatus} for event", "RabbitMQ",
                _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, "NO_SUBSCRIPTION");
        }
    }

    private void TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs, string taskId)
    {
        var consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString() ?? "0";

        _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Adding message to queue again with {Time} seconds delay...", "RabbitMQ",
            consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0", $"{_subscribeRetryTime.TotalSeconds:n1}");
        Thread.Sleep(_subscribeRetryTime);
        try
        {
            lock (ChannelAckResourceLock)
            {
                _consumerChannel?.BasicNack(eventArgs.DeliveryTag, false, true);
            }

            _logger.LogWarning("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Message added to queue again", "RabbitMQ",
                consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0");
        }
        catch (Exception ex)
        {
            _logger.LogError("{BrokerName} | {ConsumerQueue} => ConsumerChannel[ {ChannelNo} ][ {ConsumerId} ] FetcherId [ {FetcherId} ]: Could not enqueue message again: {Error}", "RabbitMQ",
                consumerQueueName, consumerChannelNumber, _currentConsumerTag, taskId ?? "0", ex.Message);
        }
    }

    private void ConsumeErrorPublish([NotNull] string errorMessage, [NotNull] string failedEventName, [NotNull] string failedMessageContent)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        ParentMessageEnvelope failedEnvelopeInfo = null;
        Type failedEventEnvelopeMessageType = null;
        IIntegrationEventMessage failedMessageObject = null;
        try
        {
            var failedEnvelope = JsonConvert.DeserializeObject<dynamic>(failedMessageContent);
            failedEnvelopeInfo = ((JObject)failedEnvelope)?.ToObject<ParentMessageEnvelope>();

            failedEventEnvelopeMessageType = _subscriptionsManager.GetEventTypeByName(failedEventName);

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(failedEventEnvelopeMessageType!);
            var @failedEventEnvelope = JsonSerializer.Deserialize(failedMessageContent, constructedClass);

            failedMessageObject = ((dynamic)failedEventEnvelope)?.Message;
        }
        catch (Exception e) { errorMessage += ". FailedMessageContent convert operation error: " + e.Message; }

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
            Channel = failedEnvelopeInfo?.Channel,
            UserId = failedEnvelopeInfo?.UserId,
            UserRoleUniqueName = failedEnvelopeInfo?.UserRoleUniqueName,
            HopLevel = failedEnvelopeInfo != null ? (ushort)(failedEnvelopeInfo.HopLevel + 1) : (ushort)1,
            ReQueuedCount = failedEnvelopeInfo?.ReQueuedCount ?? 0
        };

        var eventName = @event.Message.GetType().Name;
        eventName = EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, eventName);
        consumerErrorQueueName = $"{_rabbitMqEventBusConfig.ErrorClientInfo}_{eventName}";

        _logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] {OperationStatus}", "RabbitMQ",
            _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString(), "STARTED");

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogError("{BrokerName} | Could not publish failed event message : {Event} after {Timeout}s ({ExceptionMessage})", "RabbitMQ",
                    failedMessageContent, $"{time.TotalSeconds:n1}", ex.Message);

                // Persistent Log
                _logger.EventBusErrorLog(new ProduceMessageLogModel(
                    LogId: Guid.NewGuid().ToString(),
                    CorrelationId: @event.CorrelationId,
                    Facility: EventBusLogFacility.PRODUCE_EVENT_ERROR.ToString(),
                    ProduceDateTimeUtc: produceTime,
                    MessageLog: new MessageLogDetail(
                        EventType: eventName,
                        HopLevel: @event.HopLevel,
                        ParentMessageId: @event.ParentMessageId,
                        MessageId: @event.MessageId,
                        MessageTime: @event.MessageTime,
                        Message: @event.Message,
                        UserInfo: new EventUserDetail(
                            UserId: @event.UserId,
                            Role: @event.UserRoleUniqueName
                        )),
                    ProduceDetails: $"Message publish error: {ex.Message}"));
            });

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        policy.Execute(() =>
        {
            using var publisherChannel = _persistentConnection.CreateModel();

            publisherChannel?.QueueDeclare(queue: consumerErrorQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = publisherChannel?.CreateBasicProperties();
            properties!.DeliveryMode = (int)DeliveryMode.Persistent;

            publisherChannel.BasicPublish(
                exchange: "",
                routingKey: consumerErrorQueueName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        _logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] {OperationStatus}", "RabbitMQ",
            _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString(), "COMPLETED");
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }
}