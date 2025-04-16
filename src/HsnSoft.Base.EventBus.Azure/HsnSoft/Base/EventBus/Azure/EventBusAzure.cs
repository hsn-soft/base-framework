using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Azure.Configs;
using HsnSoft.Base.EventBus.Azure.Connection;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Azure;

public class EventBusAzure : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceBusPersisterConnection _serviceBusPersisterConnection;
    private readonly EventBusConfig _eventBusConfig;
    private readonly IEventBusLogger _logger;
    private readonly IEventBusSubscriptionManager _subsManager;
    private readonly ITraceAccesor _traceAccessor;

    private ServiceBusSender _sender;
    private ServiceBusProcessor _processor;

    public EventBusAzure(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _logger = _serviceProvider.GetRequiredService<IEventBusLogger>();

        _eventBusConfig = _serviceProvider.GetRequiredService<IOptions<AzureEventBusConfig>>().Value;
        _serviceBusPersisterConnection = _serviceProvider.GetRequiredService<IServiceBusPersisterConnection>();
        _traceAccessor = _serviceProvider.GetService<ITraceAccesor>();

        _subsManager = serviceProvider.GetService<IEventBusSubscriptionManager>();
        _subsManager.EventNameGetter = TrimEventName;

        _sender = _serviceBusPersisterConnection.TopicClient.CreateSender(_eventBusConfig.ExchangeName);
        var options = new ServiceBusProcessorOptions { MaxConcurrentCalls = 10, AutoCompleteMessages = false };
        _processor = _serviceBusPersisterConnection.TopicClient.CreateProcessor(_eventBusConfig.ExchangeName, _eventBusConfig.ConsumerClientName, options);

        RemoveDefaultRule();
        RegisterSubscriptionClientMessageHandlerAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, ParentMessageEnvelope parentMessage = null, string correlationId = null, bool isExchangeEvent = true, bool isReQueuePublish = false)
        where TEventMessage : IIntegrationEventMessage
    {
        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = eventMessage,
            Producer = _eventBusConfig.ConsumerClientInfo,
            CorrelationId = (correlationId ?? parentMessage?.CorrelationId) ?? _traceAccessor?.GetCorrelationId(),
            Channel = parentMessage?.Channel ?? _traceAccessor?.GetChannel(),
            UserId = parentMessage?.UserId,
            UserRoleUniqueName = parentMessage?.UserRoleUniqueName,
            HopLevel = parentMessage != null ? (ushort)(parentMessage.HopLevel + 1) : (ushort)1,
            ReQueuedCount = parentMessage?.ReQueuedCount ?? 0
        };
        if (isReQueuePublish)
        {
            @event.ReQueuedCount++;
        }

        _logger.LogDebug("AzureServiceBus | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _eventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        var message = new ServiceBusMessage { MessageId = Guid.NewGuid().ToString(), Body = new BinaryData(body), Subject = eventName };

        await _sender.SendMessageAsync(message);

        _logger.LogDebug("AzureServiceBus | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _eventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());
    }

    public void Subscribe<T, TH>(ushort fetchCount = 1) where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH), fetchCount);
    }

    public void Subscribe(Type eventType, Type eventHandlerType, ushort fetchCount = 1)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (!containsKey)
        {
            try
            {
                _serviceBusPersisterConnection.AdministrationClient
                    .CreateRuleAsync(_eventBusConfig.ExchangeName, _eventBusConfig.ConsumerClientName, new CreateRuleOptions { Filter = new CorrelationRuleFilter { Subject = eventName }, Name = eventName }).GetAwaiter().GetResult();
            }
            catch (ServiceBusException)
            {
                _logger.LogWarning("The messaging entity {EventName} already exists", eventName);
            }
        }

        _logger.LogInformation("AzureServiceBus | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);
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
                var eventName = $"{_eventBusConfig.EventNamePrefix ?? string.Empty}{args.Message.Subject}{_eventBusConfig.EventNameSuffix ?? string.Empty}";
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

        _logger.LogError("ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

        return Task.CompletedTask;
    }

    private void RemoveDefaultRule()
    {
        try
        {
            _serviceBusPersisterConnection
                .AdministrationClient
                .DeleteRuleAsync(_eventBusConfig.ExchangeName, _eventBusConfig.ConsumerClientName, RuleProperties.DefaultRuleName)
                .GetAwaiter()
                .GetResult();
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogWarning("The messaging entity {DefaultRuleName} Could not be found", RuleProperties.DefaultRuleName);
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

        _logger.LogDebug("Processing AzureServiceBus event: {EventName}", eventName);

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
                        _logger.LogWarning("AzureServiceBus | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _eventBusConfig.ConsumerClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        var eventType = _subsManager.GetEventInfoByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}")?.EventType;

                        Type genericClass = typeof(MessageEnvelope<>);
                        Type constructedClass = genericClass.MakeGenericType(eventType!);
                        var @event = JsonConvert.DeserializeObject(message, constructedClass);

                        _logger.LogDebug("AzureServiceBus | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : Event [ {Event} ]", _eventBusConfig.ConsumerClientInfo, eventName, @event);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                        ((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event }))!.GetAwaiter().GetResult();
                        _logger.LogDebug("AzureServiceBus | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : Event [ {Event} ]", _eventBusConfig.ConsumerClientInfo, eventName, @event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("AzureServiceBus | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _eventBusConfig.ConsumerClientInfo, eventName, ex.Message);
                    }
                }
            }

            processed = true;
        }
        else
        {
            _logger.LogWarning("AzureServiceBus | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _eventBusConfig.ConsumerClientInfo, eventName);
        }

        return processed;
    }
}