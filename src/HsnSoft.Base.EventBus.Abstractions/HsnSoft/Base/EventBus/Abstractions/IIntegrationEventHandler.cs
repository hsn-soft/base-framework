using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Abstractions;

public interface IIntegrationEventHandler<TEventMessage> : IIntegrationEventHandler
    where TEventMessage : IIntegrationEventMessage
{
    Task HandleAsync(MessageEnvelope<TEventMessage> @event);
}

public interface IIntegrationEventHandler
{
}