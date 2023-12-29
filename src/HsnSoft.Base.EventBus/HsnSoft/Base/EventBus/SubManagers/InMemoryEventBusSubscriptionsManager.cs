using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.SubManagers;

public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
    private readonly List<Type> _eventTypes;

    private readonly Func<string, string> _eventNameGetter;

    [CanBeNull]
    public event EventHandler<string> OnEventRemoved;

    public InMemoryEventBusSubscriptionsManager(Func<string, string> eventNameGetter)
    {
        _handlers = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
        _eventNameGetter = eventNameGetter;
    }

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

        if (!_eventTypes.Contains(eventType))
        {
            _eventTypes.Add(eventType);
        }
    }

    public void RemoveSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        var handlerToRemove = FindSubscriptionToRemove<T, TH>();
        var eventName = GetEventKey<T>();
        DoRemoveHandler(eventName, handlerToRemove);
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

    public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);

    public string GetEventKey<T>() where T : IIntegrationEventMessage
    {
        return GetEventKey(typeof(T));
    }

    public string GetEventKey(Type eventType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();

        var eventName = eventType.Name;
        return _eventNameGetter(eventName);
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

    private void DoRemoveHandler(string eventName, [CanBeNull] SubscriptionInfo subsToRemove)
    {
        if (subsToRemove == null) return;
        _handlers[eventName].Remove(subsToRemove);

        if (_handlers[eventName].Any()) return;
        _handlers.Remove(eventName);

        var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
        if (eventType != null)
        {
            _eventTypes.Remove(eventType);
        }

        OnEventRemoved?.Invoke(this, eventName);
    }

    [CanBeNull]
    private SubscriptionInfo FindSubscriptionToRemove<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        return DoFindSubscriptionToRemove(eventName, typeof(TH));
    }

    [CanBeNull]
    private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
    {
        return !HasSubscriptionsForEvent(eventName) ? null : _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
    }
}