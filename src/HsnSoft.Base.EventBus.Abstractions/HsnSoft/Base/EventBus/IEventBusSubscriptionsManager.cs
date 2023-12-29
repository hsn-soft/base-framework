using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }
    void Clear();
    event EventHandler<string> OnEventRemoved;

    void AddSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    void AddSubscription(Type eventType, Type eventHandlerType);

    void RemoveSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    [CanBeNull]
    Type GetEventTypeByName(string eventName);

    string GetEventKey<T>() where T : IIntegrationEventMessage;

    string GetEventKey(Type eventType);
}