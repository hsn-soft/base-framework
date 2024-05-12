using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus;

public interface IEventBusSubscriptionManager
{
    public Func<string, string> EventNameGetter { get; set; }

    bool IsEmpty { get; }
    void Clear();

    void AddSubscription<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    void AddSubscription(Type eventType, Type eventHandlerType);

    bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    [CanBeNull]
    Type GetEventTypeByName(string eventName);

    string GetEventKey<T>() where T : IIntegrationEventMessage;

    string GetEventKey(Type eventType);
}