using Edx.InvoiceService.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.FirmInvoice;

/// <summary>
/// An invoice was checked and queued by the firm invoice integration: this service takes it in (copies it, unpacks it, checks it, makes the draft). It runs as the user who sent the invoice, in the tenant of the invoice (the envelope says who), so the record is the tenant's, its
/// audit fields have the sender and the limits are the sender's.
/// </summary>
public sealed class InvoiceQueuedHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceQueuedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceQueuedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnQueuedAsync(envelope.Message, cancellationToken);
    }
}
