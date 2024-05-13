using System;
using System.Collections.Generic;
using System.Linq;
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
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

// ReSharper disable once InconsistentNaming
public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;
    private readonly ITraceAccesor _traceAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBusSubscriptionManager _subsManager;

    private static ushort MaxConsumerParallelThreadCount { get; set; } = 5;
    private static ushort MaxConsumerMaxFetchCount { get; set; } = 10;
    private readonly int _publishRetryCount = 5;
    private readonly List<RabbitMqConsumer> _consumers;
    private bool _disposed;
    private bool _publishing;

    public EventBusRabbitMq(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _logger = serviceProvider.GetRequiredService<IEventBusLogger>();
        _persistentConnection = serviceProvider.GetRequiredService<IRabbitMqPersistentConnection>();
        _traceAccessor = serviceProvider.GetService<ITraceAccesor>();
        _currentUser = serviceProvider.GetService<ICurrentUser>();

        _rabbitMqEventBusConfig = serviceProvider.GetRequiredService<IOptions<RabbitMqEventBusConfig>>().Value;
        if (_rabbitMqEventBusConfig.ConsumerParallelThreadCount > MaxConsumerParallelThreadCount)
        {
            _rabbitMqEventBusConfig.ConsumerParallelThreadCount = MaxConsumerParallelThreadCount;
        }

        if (_rabbitMqEventBusConfig.ConsumerMaxFetchCount > MaxConsumerMaxFetchCount)
        {
            _rabbitMqEventBusConfig.ConsumerMaxFetchCount = MaxConsumerMaxFetchCount;
        }

        _subsManager = serviceProvider.GetService<IEventBusSubscriptionManager>();
        _subsManager.EventNameGetter = TrimEventName;

        _consumers = new List<RabbitMqConsumer>();
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, ParentMessageEnvelope parentMessage = null, bool isReQueuePublish = false) where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _publishing = true;

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_publishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _publishing = false;
                _logger.LogError("RabbitMQ | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = eventMessage,
            Producer = _rabbitMqEventBusConfig.ClientInfo,
            CorrelationId = parentMessage?.CorrelationId ?? _traceAccessor?.GetCorrelationId(),
            Channel = parentMessage?.Channel ?? _traceAccessor?.GetChannel(),
            UserId = parentMessage?.UserId ?? _currentUser?.Id?.ToString(),
            UserRoleUniqueName = parentMessage?.UserRoleUniqueName ?? (_currentUser?.Roles is { Length: > 0 } ? _currentUser?.Roles.JoinAsString(",") : null),
            HopLevel = parentMessage != null ? parentMessage.HopLevel + 1 : 1,
            IsReQueued = isReQueuePublish || (parentMessage?.IsReQueued ?? false)
        };
        if (@event.IsReQueued)
        {
            @event.ReQueueCount = parentMessage != null ? parentMessage.ReQueueCount + 1 : 0;
        }

        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        policy.Execute(() =>
        {
            using var publisherChannel = _persistentConnection.CreateModel();

            if (!isReQueuePublish)
            {
                publisherChannel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            }
            else
            {
                // Direct re-queue, no-exchange
                publisherChannel?.QueueDeclare(queue: GetConsumerQueueName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            var properties = publisherChannel?.CreateBasicProperties();
            properties!.DeliveryMode = (int)DeliveryMode.Persistent;

            publisherChannel.BasicPublish(
                exchange: isReQueuePublish ? "" : _rabbitMqEventBusConfig.ExchangeName,
                routingKey: isReQueuePublish ? GetConsumerQueueName(eventName) : eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
        _publishing = false;
    }

    public void Subscribe<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH));
    }

    public void Subscribe(Type eventType, Type eventHandlerType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

                channel.QueueDeclare(queue: GetConsumerQueueName(eventName), //Ensure queue exists while consuming
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(queue: GetConsumerQueueName(eventName),
                    exchange: _rabbitMqEventBusConfig.ExchangeName,
                    routingKey: eventName);
            }
        }

        _logger.LogInformation("RabbitMQ | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        for (int i = 0; i < _rabbitMqEventBusConfig.ConsumerParallelThreadCount; i++)
        {
            var rabbitMqConsumer = new RabbitMqConsumer(_serviceScopeFactory, _persistentConnection, _subsManager, _rabbitMqEventBusConfig, _logger);
            rabbitMqConsumer.StartBasicConsume(eventName);

            _consumers.Add(rabbitMqConsumer);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("RabbitMQ | Terminating...");

        _logger.LogInformation("RabbitMQ | Consumers terminating...");
        Task.WaitAll(_consumers.Select(consumer => Task.Run(consumer.Dispose)).ToArray());
        _logger.LogInformation("RabbitMQ | Consumers terminated");

        _logger.LogInformation("RabbitMQ | Publisher terminating...");
        while (_publishing)
        {
            _logger.LogInformation("RabbitMQ | Publisher wait processing...");
            Thread.Sleep(1000);
        }

        _logger.LogInformation("RabbitMQ | Publisher terminated");

        _subsManager.Clear();
        _consumers.Clear();

        if (_persistentConnection?.IsConnected == true)
        {
            _persistentConnection?.Dispose();
        }

        _logger.LogInformation("RabbitMQ | Terminated");
    }

    [CanBeNull]
    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel?.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_rabbitMqEventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_rabbitMqEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_rabbitMqEventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_rabbitMqEventBusConfig.EventNamePrefix.Length);
        }

        if (_rabbitMqEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_rabbitMqEventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _rabbitMqEventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }
}