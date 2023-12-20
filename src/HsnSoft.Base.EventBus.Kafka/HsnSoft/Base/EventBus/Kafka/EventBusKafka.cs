﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.Kafka;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusKafka> _logger;
    private readonly KafkaConnectionSettings _kafkaConnectionSettings;
    private readonly EventBusConfig _eventBusConfig;

    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly CancellationTokenSource _tokenSource;
    private readonly List<Task> _consumerTasks;
    private readonly List<Task> _messageProcessorTasks;

    public EventBusKafka(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<EventBusKafka>();

        _kafkaConnectionSettings = _serviceProvider.GetRequiredService<IOptions<KafkaConnectionSettings>>().Value;
        _eventBusConfig = _serviceProvider.GetRequiredService<IOptions<EventBusConfig>>().Value;

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _tokenSource = new CancellationTokenSource();
        _consumerTasks = new List<Task>();
        _messageProcessorTasks = new List<Task>();
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        var kafkaProducer = new KafkaProducer(_kafkaConnectionSettings, _eventBusConfig, _logger);
        await kafkaProducer.StartSendingMessages(eventName, @event);
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

        _logger.LogInformation("Kafka | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        _consumerTasks.Add(Task.Run(() =>
        {
            var kafkaConsumer = new KafkaConsumer(_kafkaConnectionSettings, _eventBusConfig, _logger);
            kafkaConsumer.OnMessageReceived += OnMessageReceived;
            kafkaConsumer.StartReceivingMessages(eventType, eventName, _tokenSource.Token);
        }));
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
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

    private void OnMessageReceived([CanBeNull] object sender, object message)
    {
        _messageProcessorTasks.Add(Task.Run(async () =>
        {
            var eventName = message.GetType().Name;
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
                        _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _eventBusConfig.ClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        _logger.LogInformation("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : EventId [ {EventId} ]", _eventBusConfig.ClientInfo, eventName, ((message as IntegrationEvent)!).Id.ToString());
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(message.GetType());
                        await ((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { message })!)!;
                        _logger.LogInformation("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : EventId [ {EventId} ]", _eventBusConfig.ClientInfo, eventName, ((message as IntegrationEvent)!).Id.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _eventBusConfig.ClientInfo, eventName, ex.Message);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _eventBusConfig.ClientInfo, eventName);
            }
        }));
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
}