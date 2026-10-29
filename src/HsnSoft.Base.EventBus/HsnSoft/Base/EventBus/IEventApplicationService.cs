using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>For application services that publish on behalf of the message being handled: the handler hands the message over once.</summary>
public interface IEventApplicationService
{
    void SetParentIntegrationEvent<T>(MessageEnvelope<T> envelope)
        where T : IIntegrationEventMessage;
}
