using Edx.Integration.GibInvoice.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.Integration.GibInvoice.Http.Host.EventHandlers.Invoicing;

/// <summary>An invoice was approved and signed by the invoice service: it is put in an envelope for the center. It runs as the tenant of the invoice (the envelope of the event says which).</summary>
public sealed class InvoiceSignedHandler(OutboundEnvelopeMaker maker) : IIntegrationEventHandler<InvoiceSignedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceSignedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return maker.OnInvoiceSignedAsync(envelope.Message, cancellationToken);
    }
}
