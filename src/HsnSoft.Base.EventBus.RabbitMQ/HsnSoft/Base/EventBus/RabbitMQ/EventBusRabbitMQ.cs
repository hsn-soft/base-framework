using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public class EventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly EventBusConfig _eventBusConfig;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;

    private IModel _consumerChannel;

    public EventBusRabbitMQ(IServiceProvider serviceProvider, IRabbitMQPersistentConnection persistentConnection,
        EventBusConfig eventBusConfig, ILogger<EventBusRabbitMQ> logger)
    {
        _serviceProvider = serviceProvider;
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _eventBusConfig = eventBusConfig ?? new EventBusConfig();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannel();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public void Publish(IntegrationEvent @event)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_eventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            channel.ExchangeDeclare(exchange: _eventBusConfig.DefaultTopicName, type: "direct"); //Ensure exchange exists while publishing

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

                channel.BasicPublish(
                    exchange: _eventBusConfig.DefaultTopicName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        eventName = TrimEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel.QueueDeclare(queue: GetSubName(eventName), //Ensure queue exists while consuming
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _consumerChannel.QueueBind(queue: GetSubName(eventName),
                exchange: _eventBusConfig.DefaultTopicName,
                routingKey: eventName);
        }

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        _subsManager.AddSubscription<T, TH>();
        StartBasicConsume(eventName);
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        if (_consumerChannel != null)
        {
            _consumerChannel.Dispose();
        }

        _subsManager.Clear();
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueUnbind(queue: GetSubName(eventName),
                exchange: _eventBusConfig.DefaultTopicName,
                routingKey: TrimEventName(eventName));

            if (_subsManager.IsEmpty)
            {
                _consumerChannel.Close();
            }
        }
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _eventBusConfig.DefaultTopicName, type: "direct");

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        _logger.LogTrace("Starting RabbitMQ basic consume");

        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += ConsumerReceived;

            _consumerChannel.BasicConsume(
                queue: GetSubName(eventName),
                autoAck: false,
                consumer: consumer);
        }
        else
        {
            _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async void ConsumerReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        eventName = TrimEventName(eventName);
        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        try
        {
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
    }

    private string GetSubName(string eventName)
    {
        return $"{_eventBusConfig.SubscriberClientAppName}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_eventBusConfig.DeleteEventPrefix && eventName.StartsWith(_eventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_eventBusConfig.EventNamePrefix.Length);
        }

        if (_eventBusConfig.DeleteEventSuffix && eventName.EndsWith(_eventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _eventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }

    private async Task<bool> ProcessEvent(string eventName, string message)
    {
        eventName = TrimEventName(eventName);

        var processed = false;

        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = _serviceProvider.GetService(subscription.HandlerType);
                    if (handler == null) continue;

                    var eventType = _subsManager.GetEventTypeByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
                }
            }

            processed = true;
        }
        else
        {
            _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
        }

        return processed;
    }
}