using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;
    private readonly IEventBusSubscriptionManager _subsManager;

    private readonly int _publishRetryCount = 5;
    private readonly List<RabbitMqConsumer> _consumers = [];

    private int _isPublishing;
    private bool _disposed;

    public EventBusRabbitMq(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<IEventBusLogger>();
        _persistentConnection = serviceProvider.GetRequiredService<IRabbitMqPersistentConnection>();
        _rabbitMqEventBusConfig = serviceProvider.GetRequiredService<IOptions<RabbitMqEventBusConfig>>().Value;
        _subsManager = serviceProvider.GetRequiredService<IEventBusSubscriptionManager>();
        _subsManager.EventNameGetter = TrimEventName;
    }


    public async Task PublishAsync<TEventMessage>(
        TEventMessage eventMessage,
        ParentMessageEnvelope parentMessage = null,
        string correlationId = null,
        bool isExchangeEvent = true,
        bool isReQueuePublish = false)
        where TEventMessage : IIntegrationEventMessage
    {
        using var scope = _serviceProvider.CreateScope(); // ✅ scope for scoped services
        var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
        var traceAccessor = scope.ServiceProvider.GetService<ITraceAccesor>();

        await EnsureConnectedAsync();

        Interlocked.Exchange(ref _isPublishing, 1);

        string eventName = TrimEventName(eventMessage.GetType().Name);
        var produceTime = DateTime.UtcNow;

        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = produceTime,
            Message = eventMessage,
            Producer = _rabbitMqEventBusConfig.ConsumerClientInfo,

            CorrelationId = (correlationId ?? parentMessage?.CorrelationId) ?? traceAccessor?.GetCorrelationId(),
            UserId = parentMessage?.UserId ?? currentUser?.Id?.ToString(),
            UserRoles = parentMessage?.UserRoles ?? (currentUser?.Roles is { Length: > 0 } ? currentUser?.Roles.JoinAsString(",") : null),
            ClientLat = parentMessage?.ClientLat ?? traceAccessor?.GetClientLat(),
            ClientLong = parentMessage?.ClientLong ?? traceAccessor?.GetClientLong(),
            ClientChannel = parentMessage?.ClientChannel ?? traceAccessor?.GetClientChannel(),
            ClientVersion = parentMessage?.ClientVersion ?? traceAccessor?.GetClientVersion(),

            HopLevel = parentMessage != null ? (ushort)(parentMessage.HopLevel + 1) : (ushort)1,
            ReQueuedCount = (ushort)((parentMessage?.ReQueuedCount ?? 0) + (isReQueuePublish ? 1 : 0))
        };

        _logger.LogDebug("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] STARTED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId);

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(_publishRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    _logger.LogError("{BrokerName} | Publish retry after {Timeout}s ({Message})",
                        "RabbitMQ", time.TotalSeconds, ex.Message);

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

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(),
            new JsonSerializerOptions { WriteIndented = true });

        await policy.ExecuteAsync(async () =>
        {
            await using var publisherChannel = await _persistentConnection.CreateModelAsync();

            string publishQueueName = string.Empty;

            if (!isReQueuePublish && isExchangeEvent)
            {
                await publisherChannel.ExchangeDeclareAsync(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            }
            else
            {
                publishQueueName = eventName.Equals("ReQueued")
                    ? EventNameHelper.GetConsumerReQueuedEventQueueName(
                        (eventMessage as ReQueuedEto)?.ReQueuedMessageEnvelopeConsumer, eventName)
                    : EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, eventName);

                await publisherChannel.QueueDeclareAsync(queue: publishQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            await publisherChannel.BasicPublishAsync(
                exchange: !isReQueuePublish && isExchangeEvent ? _rabbitMqEventBusConfig.ExchangeName : "",
                routingKey: !isReQueuePublish && isExchangeEvent ? eventName : publishQueueName,
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        });

        _logger.LogDebug("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId);

        Interlocked.Exchange(ref _isPublishing, 0);
    }

    public void Subscribe<TEvent, THandler>(ushort fetchCount = 1)
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>
        => Subscribe(typeof(TEvent), typeof(THandler), fetchCount);

    public void Subscribe(Type eventType, Type eventHandlerType, ushort fetchCount = 1)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage)))
            throw new TypeAccessException($"{eventType.Name} is not a valid IIntegrationEventMessage");

        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler)))
            throw new TypeAccessException($"{eventHandlerType.Name} is not a valid IIntegrationEventHandler");

        string eventName = TrimEventName(eventType.Name);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("{BrokerName} | Already subscribed to {EventName}", "RabbitMQ", eventName);
            return;
        }

        AddQueueBindForEventSubscriptionAsync(eventName).GetAwaiter().GetResult();

        _logger.LogDebug("{BrokerName} | Subscribing to event {EventName} with {EventHandler}", "RabbitMQ", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType, fetchCount);

        var consumer = new RabbitMqConsumer(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _persistentConnection,
            _subsManager,
            _rabbitMqEventBusConfig,
            _logger,
            eventName);

        consumer.StartBasicConsume().GetAwaiter().GetResult();

        _consumers.Add(consumer);
    }

    private async Task AddQueueBindForEventSubscriptionAsync(string eventName)
    {
        await EnsureConnectedAsync();

        string consumerQueueName = EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, eventName);
        await using var channel = await _persistentConnection.CreateModelAsync();

        //Ensure exchange exists while consuming
        await channel.ExchangeDeclareAsync(_rabbitMqEventBusConfig.ExchangeName, "direct");

        //Ensure queue exists while consuming
        await channel.QueueDeclareAsync(consumerQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        await channel.QueueBindAsync(consumerQueueName, _rabbitMqEventBusConfig.ExchangeName, eventName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("{BrokerName} | {OperationStatus}", "RabbitMQ", "TERMINATING");

        _logger.LogDebug("{BrokerName} | Consumers terminating...", "RabbitMQ");
        foreach (var consumer in _consumers)
        {
            try
            {
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError("{BrokerName} | Consumer dispose error: {Error}", "RabbitMQ", ex.Message);
            }
        }

        _logger.LogDebug("{BrokerName} | Consumers terminated", "RabbitMQ");

        _logger.LogDebug("{BrokerName} | Publisher terminating...", "RabbitMQ");
        int waitCount = 0;
        while (Interlocked.CompareExchange(ref _isPublishing, 0, 0) == 1 && waitCount < 30)
        {
            _logger.LogDebug("{BrokerName} | Waiting publisher to finish...", "RabbitMQ");
            Thread.Sleep(1000);
            waitCount++;
        }

        _logger.LogDebug("{BrokerName} | Publisher terminated", "RabbitMQ");

        _subsManager.Clear();
        _consumers.Clear();

        if (_persistentConnection.IsConnected)
            _persistentConnection.Dispose();

        _logger.LogInformation("{BrokerName} | {OperationStatus}", "RabbitMQ", "TERMINATED");
    }

    private string TrimEventName(string eventName)
        => EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, eventName);

    private async Task EnsureConnectedAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
            if (!_persistentConnection.IsConnected)
                throw new InvalidOperationException("RabbitMQ connection could not be established.");
        }
    }
}