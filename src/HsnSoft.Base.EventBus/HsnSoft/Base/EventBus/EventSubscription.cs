using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus;

/// <summary>Tells a broker project which event type the service listens to and how to hand a message to its handler.</summary>
/// <param name="EventType">The event the service subscribed to.</param>
/// <param name="Handle">Resolves the handler from the given (per-message) scope and runs it.</param>
public sealed record EventSubscription(Type EventType, Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Handle);
