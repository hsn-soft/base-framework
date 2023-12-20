using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.RabbitMQ;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

// ReSharper disable once InconsistentNaming
public class EventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly EventBusConfig _eventBusConfig;
    private readonly RabbitMQConnectionSettings _rabbitMqConnectionSettings;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;

    [CanBeNull]
    private readonly IModel _consumerChannel;

    public EventBusRabbitMQ(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<EventBusRabbitMQ>();

        _rabbitMqConnectionSettings = _serviceProvider.GetRequiredService<IOptions<RabbitMQConnectionSettings>>().Value;
        _eventBusConfig = _serviceProvider.GetRequiredService<IOptions<EventBusConfig>>().Value;
        _persistentConnection = _serviceProvider.GetRequiredService<IRabbitMQPersistentConnection>();

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannel();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public Task PublishAsync(IntegrationEvent @event)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_rabbitMqConnectionSettings.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "RabbitMQ | Could not publish event: {Event} after {Timeout}s ({ExceptionMessage})", @event, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | Creating channel to publish event: {Event} ({EventName})", @event, eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogDebug("RabbitMQ | Declaring exchange to publish event: {Event}", @event);

            channel.ExchangeDeclare(exchange: _eventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing

            _logger.LogInformation("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => EventId [ {EventId} ] STARTED", _eventBusConfig.ClientInfo, eventName, @event.Id.ToString());

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

            policy.Execute(() =>
            {
                var properties = channel?.CreateBasicProperties();
                properties!.DeliveryMode = 2; // persistent

                _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);

                channel.BasicPublish(
                    exchange: _eventBusConfig.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            _logger.LogInformation("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => EventId [ {EventId} ] COMPLETED", _eventBusConfig.ClientInfo, eventName, @event.Id.ToString());
        }

        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH));
    }

    public void Subscribe(Type eventType, Type eventHandlerType)
    {
        if (!eventType.IsAssignableTo(typeof(IntegrationEvent))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel?.QueueDeclare(queue: GetConsumerQueueName(eventName), //Ensure queue exists while consuming
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _consumerChannel?.QueueBind(queue: GetConsumerQueueName(eventName),
                exchange: _eventBusConfig.ExchangeName,
                routingKey: eventName);
        }

        _logger.LogInformation("RabbitMQ | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);
        StartBasicConsume(eventName);
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("RabbitMQ | Unsubscribing from event {EventName}", eventName);

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

    private void SubsManager_OnEventRemoved([CanBeNull] object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueUnbind(queue: GetConsumerQueueName(eventName),
                exchange: _eventBusConfig.ExchangeName,
                routingKey: TrimEventName(eventName));

            if (_subsManager.IsEmpty)
            {
                _consumerChannel?.Close();
            }
        }
    }

    [CanBeNull]
    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel?.ExchangeDeclare(exchange: _eventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _eventBusConfig.ClientInfo, eventName);

        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += ConsumerReceived;

            _consumerChannel.BasicConsume(
                queue: GetConsumerQueueName(eventName),
                autoAck: false,
                consumer: consumer);
        }
        else
        {
            _logger.LogError("RabbitMQ | StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async void ConsumerReceived([CanBeNull] object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _eventBusConfig.ClientInfo, eventName);
        try
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Message}", _eventBusConfig.ClientInfo, eventName, message);
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _eventBusConfig.ClientInfo, eventName, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume COMPLETED", _eventBusConfig.ClientInfo, eventName);
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_eventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
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

    private async Task ProcessEvent(string eventName, string message)
    {
        eventName = TrimEventName(eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = _serviceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _eventBusConfig.ClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        var eventType = _subsManager.GetEventTypeByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType!);

                        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : EventId [ {EventId} ]", _eventBusConfig.ClientInfo, eventName, ((integrationEvent as IntegrationEvent)!).Id.ToString());
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                        await ((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { integrationEvent })!)!;
                        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : EventId [ {EventId} ]", _eventBusConfig.ClientInfo, eventName, ((integrationEvent as IntegrationEvent)!).Id.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _eventBusConfig.ClientInfo, eventName, ex.Message);
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _eventBusConfig.ClientInfo, eventName);
        }
    }
}