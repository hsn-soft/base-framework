using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>
/// Knows which events the service listens to and who handles them. A service has one handler per event: when several are registered
/// for the same event name, the first wins and the others are ignored (that is how a service's own handler for an event replaces a
/// shared default).
/// </summary>
public interface IEventBusSubscriptionManager
{
    /// <summary>Turns a type name into an event name (removes the configured prefix and suffix).</summary>
    Func<string, string> EventNameGetter { get; }

    bool IsEmpty { get; }

    /// <summary>Every event the service listens to, in registration order.</summary>
    IReadOnlyList<IntegrationEventInfo> Events { get; }

    bool HasSubscriptionsForEvent<T>()
        where T : IIntegrationEventMessage;

    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent<T>()
        where T : IIntegrationEventMessage;

    IEnumerable<IntegrationEventHandlerInfo> GetHandlersForEvent(string eventName);

    IntegrationEventInfo? GetEventInfoByName(string eventName);

    string GetEventKey<T>()
        where T : IIntegrationEventMessage;

    string GetEventKey(Type eventType);
}
