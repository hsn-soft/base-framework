#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.Kafka;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusKafka> _logger;
    private readonly KafkaConnectionSettings _kafkaConnectionSettings;
    private readonly KafkaEventBusConfig _kafkaEventBusConfig;
    private readonly IEventBusTraceAccesor _traceAccessor;

    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly CancellationTokenSource _tokenSource;
    private readonly List<Task> _consumerTasks;
    private readonly List<Task> _messageProcessorTasks;

    public EventBusKafka(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<EventBusKafka>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        _kafkaConnectionSettings = _serviceProvider.GetRequiredService<IOptions<KafkaConnectionSettings>>().Value;
        _kafkaEventBusConfig = _serviceProvider.GetRequiredService<IOptions<KafkaEventBusConfig>>().Value;
        _traceAccessor = _serviceProvider.GetService<IEventBusTraceAccesor>();

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _tokenSource = new CancellationTokenSource();
        _consumerTasks = new List<Task>();
        _messageProcessorTasks = new List<Task>();
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, Guid? parentMessageId = null, string? correlationId = null) where TEventMessage : IIntegrationEventMessage
    {
        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var kafkaProducer = new KafkaProducer(_kafkaConnectionSettings, _kafkaEventBusConfig, _logger);

        var message = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = eventMessage,
            Producer = _kafkaEventBusConfig.ClientInfo,
            CorrelationId = correlationId ?? _traceAccessor.GetCorrelationId()
        };

        await kafkaProducer.StartSendingMessages(eventName, message);
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

        _logger.LogInformation("Kafka | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        _consumerTasks.Add(Task.Run(() =>
        {
            var kafkaConsumer = new KafkaConsumer(_kafkaConnectionSettings, _kafkaEventBusConfig, _logger);
            kafkaConsumer.OnMessageReceived += OnMessageReceived;
            kafkaConsumer.StartReceivingMessages(eventType, eventName, _tokenSource.Token);
        }));
    }

    public void Unsubscribe<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Kafka | Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        _logger.LogInformation("Message Broker Bridge shutting down...");

        _tokenSource.Cancel();
        _tokenSource.Dispose();

        _consumerTasks.RemoveAll(x => x.IsCompleted);
        if (_consumerTasks.Count > 0)
        {
            _logger.LogInformation("Consumer Task Count [ {ConsumerTasksCount} ]", _consumerTasks.Count);

            // Waiting all tasks to finishing their jobs until finish
            Task.WaitAll(_consumerTasks.ToArray());
        }

        _messageProcessorTasks.RemoveAll(x => x.IsCompleted);
        if (_messageProcessorTasks.Count > 0)
        {
            _logger.LogInformation("Message Processor Task Count [ {ProcessorTasks} ]", _messageProcessorTasks.Count);

            // Waiting all tasks to finishing their jobs, but if task processing more time 30 seconds continue
            Task.WaitAll(_messageProcessorTasks.ToArray(), 30000);
        }

        _subsManager.Clear();

        _logger.LogInformation("Message Broker Bridge terminated");
    }

    private void OnMessageReceived([CanBeNull] object sender, KeyValuePair<Type, string> messageObject)
    {
        _messageProcessorTasks.Add(Task.Run(async () =>
        {
            var eventName = messageObject.Key.Name;
            eventName = TrimEventName(eventName);

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);

                using var scope = _serviceProvider.CreateScope();
                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _kafkaEventBusConfig.ClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        Type genericClass = typeof(MessageEnvelope<>);
                        Type constructedClass = genericClass.MakeGenericType(messageObject.Key);
                        var @event = JsonConvert.DeserializeObject(messageObject.Value, constructedClass);

                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : Event [ {Event} ]", _kafkaEventBusConfig.ClientInfo, eventName, @event);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(messageObject.Key);
                        await (Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event })!;
                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : Event [ {Event} ]", _kafkaEventBusConfig.ClientInfo, eventName, @event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _kafkaEventBusConfig.ClientInfo, eventName, ex.Message);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _kafkaEventBusConfig.ClientInfo, eventName);
            }
        }));
    }

    private string TrimEventName(string eventName)
    {
        if (_kafkaEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_kafkaEventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_kafkaEventBusConfig.EventNamePrefix.Length);
        }

        if (_kafkaEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_kafkaEventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _kafkaEventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }
}