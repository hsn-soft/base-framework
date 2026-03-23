using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

public interface IEventApplicationService
{
    public void SetParentIntegrationEvent<T>(MessageEnvelope<T> @event) where T : IIntegrationEventMessage;
}