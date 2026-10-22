using Xyz.Shared.Events;

namespace Xyz.Shared.Messaging;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}

public interface IEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}

/// <summary>Tells the subscriber which event a service listens to and how to hand it to the handler.</summary>
public sealed record EventSubscription(Type EventType, Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Handle);
