using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.SubManagers;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();

    public Func<string, string> EventNameGetter { get; set; }
    public bool IsEmpty => _handlers is { Count: 0 };
    public void Clear() => _handlers.Clear();

    public void AddSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        AddSubscription(typeof(T), typeof(TH));
    }

    public void AddSubscription(Type eventType, Type eventHandlerType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = GetEventKey(eventType);

        DoAddSubscription(eventHandlerType, eventName);

        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes[eventName] = eventType;
        }
    }

    public bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage
    {
        var key = GetEventKey<T>();
        return HasSubscriptionsForEvent(key);
    }

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage
    {
        var key = GetEventKey<T>();
        return GetHandlersForEvent(key);
    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    public Type GetEventTypeByName(string eventName) => _eventTypes[eventName];

    public string GetEventKey<T>() where T : IIntegrationEventMessage
    {
        return GetEventKey(typeof(T));
    }

    public string GetEventKey(Type eventType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();

        var eventName = eventType.Name;
        return EventNameGetter(eventName);
    }

    private void DoAddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }

        _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
    }
}