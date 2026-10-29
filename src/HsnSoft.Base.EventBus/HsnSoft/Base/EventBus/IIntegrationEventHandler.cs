using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>Reacts to one kind of event. A new instance (in its own scope) handles each message.</summary>
public interface IIntegrationEventHandler<TEventMessage> : IIntegrationEventHandler
    where TEventMessage : IIntegrationEventMessage
{
    Task HandleAsync(MessageEnvelope<TEventMessage> envelope, CancellationToken cancellationToken);
}

/// <summary>Marks every handler, so they can be found by scanning an assembly.</summary>
public interface IIntegrationEventHandler;
