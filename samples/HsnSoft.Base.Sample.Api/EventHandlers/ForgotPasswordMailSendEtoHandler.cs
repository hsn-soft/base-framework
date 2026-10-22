using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Sample.Api.Application.Contracts.Events;

namespace HsnSoft.Base.Sample.Api.EventHandlers;

public sealed class ForgotPasswordMailSendEtoHandler(ReceivedEventStore store) : IIntegrationEventHandler<ForgotPasswordMailSendEto>
{
    public Task HandleAsync(MessageEnvelope<ForgotPasswordMailSendEto> @event)
    {
        // A real service would send the mail here.
        store.Add(@event);
        return Task.CompletedTask;
    }
}
