using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.SubManagers;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<IntegrationEventHandlerInfo>> _subsHandlerList = new();
    private readonly Dictionary<string, IntegrationEventInfo> _eventList = new();

    public Func<string, string> EventNameGetter { get; set; }
    public bool IsEmpty => _subsHandlerList is { Count: 0 };
    public void Clear() => _subsHandlerList.Clear();

    public void AddSubscription<T, TH>(ushort fetchCount = 1) where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        AddSubscription(typeof(T), typeof(TH), fetchCount);
    }

    public void AddSubscription(Type eventType, Type eventHandlerType, ushort fetchCount = 1)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = GetEventKey(eventType);

        DoAddSubscription(eventHandlerType, eventName);

        if (!_eventList.ContainsKey(eventName))
        {
            _eventList[eventName] = IntegrationEventInfo.Typed(eventType, fetchCount);
        }
    }

    public bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage
    {
        var key = GetEventKey<T>();
        return HasSubscriptionsForEvent(key);
    }

    public bool HasSubscriptionsForEvent(string eventName) => _subsHandlerList.ContainsKey(eventName);

    public IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage
    {
        var key = GetEventKey<T>();
        return GetHandlersForEvent(key);
    }

    public IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent(string eventName) => _subsHandlerList[eventName];

    public IntegrationEventInfo GetEventInfoByName(string eventName) => _eventList[eventName];

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
            _subsHandlerList.Add(eventName, new List<IntegrationEventHandlerInfo>());
        }

        if (_subsHandlerList[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }

        _subsHandlerList[eventName].Add(IntegrationEventHandlerInfo.Typed(handlerType));
    }
}