using Edx.Integration.GibInvoice.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.Integration.GibInvoice.Http.Host.EventHandlers.Invoicing;

/// <summary>The answer of a tenant to a commercial invoice was signed: it is put in an envelope for the center. It runs as the tenant that answered.</summary>
public sealed class PurchaseAnswerSignedHandler(OutboundEnvelopeMaker maker) : IIntegrationEventHandler<PurchaseAnswerSignedEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseAnswerSignedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return maker.OnPurchaseAnswerSignedAsync(envelope.Message, cancellationToken);
    }
}
