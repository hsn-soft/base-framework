using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus;

/// <summary>Publishes events. RabbitMQ, Kafka or any other broker implements it in its own project.</summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}

/// <summary>Reacts to one kind of event. A new handler instance (and scope) is used per message.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
