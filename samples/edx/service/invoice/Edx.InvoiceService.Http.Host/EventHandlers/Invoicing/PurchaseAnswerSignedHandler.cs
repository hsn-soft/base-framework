using Edx.InvoiceService.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.Invoicing;

/// <summary>The signer service signed the answer to a purchase invoice: this service keeps it with the invoice. It runs as the person who answered.</summary>
public sealed class PurchaseAnswerSignedHandler(PurchaseInvoiceReceiver receiver) : IIntegrationEventHandler<PurchaseAnswerSignedEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseAnswerSignedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receiver.OnAnswerSignedAsync(envelope.Message, cancellationToken);
    }
}
