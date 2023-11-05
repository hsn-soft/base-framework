using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using HsnSoft.Base.AzureServiceBus;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using HsnSoft.Base.EventBus.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Azure;

public class EventBusServiceBus : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceBusPersisterConnection _serviceBusPersisterConnection;
    private readonly EventBusConfig _eventBusConfig;
    private readonly ILogger<EventBusServiceBus> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;

    private ServiceBusSender _sender;
    private ServiceBusProcessor _processor;

    public EventBusServiceBus(IServiceProvider serviceProvider, IServiceBusPersisterConnection serviceBusPersisterConnection,
        EventBusConfig eventBusConfig, ILogger<EventBusServiceBus> logger)
    {
        _serviceProvider = serviceProvider;
        _serviceBusPersisterConnection = serviceBusPersisterConnection;
        _eventBusConfig = eventBusConfig ?? new EventBusConfig();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _sender = _serviceBusPersisterConnection.TopicClient.CreateSender(_eventBusConfig.DefaultTopicName);
        var options = new ServiceBusProcessorOptions { MaxConcurrentCalls = 10, AutoCompleteMessages = false };
        _processor = _serviceBusPersisterConnection.TopicClient.CreateProcessor(_eventBusConfig.DefaultTopicName, _eventBusConfig.SubscriberClientAppName, options);

        RemoveDefaultRule();
        RegisterSubscriptionClientMessageHandlerAsync().GetAwaiter().GetResult();
    }

    public void Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        var message = new ServiceBusMessage { MessageId = Guid.NewGuid().ToString(), Body = new BinaryData(body), Subject = eventName };

        _sender.SendMessageAsync(message)
            .GetAwaiter()
            .GetResult();
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        eventName = TrimEventName(eventName);

        var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
        if (!containsKey)
        {
            try
            {
                _serviceBusPersisterConnection.AdministrationClient.CreateRuleAsync(_eventBusConfig.DefaultTopicName, _eventBusConfig.SubscriberClientAppName, new CreateRuleOptions { Filter = new CorrelationRuleFilter { Subject = eventName }, Name = eventName }).GetAwaiter().GetResult();
            }
            catch (ServiceBusException)
            {
                _logger.LogWarning("The messaging entity {eventName} already exists.", eventName);
            }
        }

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        _subsManager.AddSubscription<T, TH>();
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        eventName = TrimEventName(eventName);
        try
        {
            _serviceBusPersisterConnection
                .AdministrationClient
                .DeleteRuleAsync(_eventBusConfig.DefaultTopicName, _eventBusConfig.SubscriberClientAppName, eventName)
                .GetAwaiter()
                .GetResult();
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogWarning("The messaging entity {eventName} Could not be found.", eventName);
        }

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        _subsManager.Clear();
        _processor.CloseAsync().GetAwaiter().GetResult();
    }

    private async Task RegisterSubscriptionClientMessageHandlerAsync()
    {
        _processor.ProcessMessageAsync +=
            async (args) =>
            {
                var eventName = $"{(_eventBusConfig.EventNamePrefix ?? string.Empty)}{args.Message.Subject}{(_eventBusConfig.EventNameSuffix ?? string.Empty)}";
                var messageData = args.Message.Body.ToString();

                // Complete the message so that it is not received again.
                if (await ProcessEvent(eventName, messageData))
                {
                    await args.CompleteMessageAsync(args.Message);
                }
            };

        _processor.ProcessErrorAsync += ErrorHandler;
        await _processor.StartProcessingAsync();
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        var ex = args.Exception;
        var context = args.ErrorSource;

        _logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

        return Task.CompletedTask;
    }

    private void RemoveDefaultRule()
    {
        try
        {
            _serviceBusPersisterConnection
                .AdministrationClient
                .DeleteRuleAsync(_eventBusConfig.DefaultTopicName, _eventBusConfig.SubscriberClientAppName, RuleProperties.DefaultRuleName)
                .GetAwaiter()
                .GetResult();
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogWarning("The messaging entity {DefaultRuleName} Could not be found.", RuleProperties.DefaultRuleName);
        }
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

        _logger.LogTrace("Processing AzureServiceBus event: {EventName}", eventName);

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
            _logger.LogWarning("No subscription for AzureServiceBus event: {EventName}", eventName);
        }

        return processed;
    }
}