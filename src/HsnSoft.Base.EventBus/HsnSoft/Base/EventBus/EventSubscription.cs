using System;

namespace HsnSoft.Base.EventBus;

/// <summary>One registered handler: registered with <c>AddEventHandler</c>, read by the subscription manager.</summary>
/// <param name="EventType">The event the handler listens to.</param>
/// <param name="HandlerType">The handler.</param>
/// <param name="FetchCount">How many messages of the event are handled at the same time.</param>
public sealed record EventSubscription(Type EventType, Type HandlerType, ushort FetchCount);

/// <summary>What a consumer needs to know about an event the service listens to.</summary>
/// <param name="EventName">The name on the broker (the type name without prefix and suffix).</param>
/// <param name="EventType">The event type.</param>
/// <param name="FetchCount">How many messages of the event are handled at the same time.</param>
public sealed record IntegrationEventInfo(string EventName, Type EventType, ushort FetchCount);

/// <summary>The handler of an event.</summary>
/// <param name="HandlerType">The handler type.</param>
public sealed record IntegrationEventHandlerInfo(Type HandlerType);
