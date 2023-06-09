using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Abstractions;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }
    void Clear();
    event EventHandler<string> OnEventRemoved;

    void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    void RemoveSubscription<T, TH>() where TH : IIntegrationEventHandler<T> where T : IntegrationEvent;

    bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    Type GetEventTypeByName(string eventName);

    string GetEventKey<T>();
}