using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

public sealed partial class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly EventBusOptions _options;
    private readonly List<IntegrationEventInfo> _events = [];
    private readonly Dictionary<string, IntegrationEventHandlerInfo> _handlers = [];
    private readonly Dictionary<string, IntegrationEventInfo> _byName = [];

    public InMemoryEventBusSubscriptionManager(
        IEnumerable<EventSubscription> subscriptions,
        IOptions<EventBusOptions> options,
        ILogger<InMemoryEventBusSubscriptionManager> logger)
    {
        _options = options.Value;

        foreach (EventSubscription subscription in subscriptions)
        {
            string eventName = GetEventKey(subscription.EventType);
            if (_byName.ContainsKey(eventName))
            {
                Type existing = _handlers[eventName].HandlerType;
                if (existing != subscription.HandlerType)
                {
                    LogAlreadySubscribed(logger, eventName, subscription.HandlerType.Name, existing.Name);
                }

                continue;
            }

            IntegrationEventInfo info = new(eventName, subscription.EventType, subscription.FetchCount < 1 ? (ushort)1 : subscription.FetchCount);
            _byName[eventName] = info;
            _handlers[eventName] = new IntegrationEventHandlerInfo(subscription.HandlerType);
            _events.Add(info);
        }
    }

    public Func<string, string> EventNameGetter => typeName => EventNames.Trim(_options, typeName);

    public bool IsEmpty => _events.Count == 0;

    public IReadOnlyList<IntegrationEventInfo> Events => _events;

    public bool HasSubscriptionsForEvent<T>()
        where T : IIntegrationEventMessage
        => HasSubscriptionsForEvent(GetEventKey<T>());

    public bool HasSubscriptionsForEvent(string eventName) => _byName.ContainsKey(eventName);

    public IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent<T>()
        where T : IIntegrationEventMessage
        => GetHandlersForEvent(GetEventKey<T>());

    public IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent(string eventName)
        => _handlers.TryGetValue(eventName, out IntegrationEventHandlerInfo? handler) ? [handler] : [];

    public IntegrationEventInfo? GetEventInfoByName(string eventName)
        => _byName.GetValueOrDefault(eventName);

    public string GetEventKey<T>()
        where T : IIntegrationEventMessage
        => GetEventKey(typeof(T));

    public string GetEventKey(Type eventType) => EventNames.For(_options, eventType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Event {EventName} already has a handler: {Existing} stays, {Ignored} is ignored")]
    private static partial void LogAlreadySubscribed(ILogger logger, string eventName, string ignored, string existing);
}
