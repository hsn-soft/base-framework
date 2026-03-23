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

    void AddSubscription<T, TH>(ushort fetchCount = 1) where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>;

    void AddSubscription(Type eventType, Type eventHandlerType, ushort fetchCount = 1);

    bool HasSubscriptionsForEvent<T>() where T : IIntegrationEventMessage;
    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent<T>() where T : IIntegrationEventMessage;
    IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent(string eventName);

    [CanBeNull]
    IntegrationEventInfo GetEventInfoByName(string eventName);

    string GetEventKey<T>() where T : IIntegrationEventMessage;

    string GetEventKey(Type eventType);
}