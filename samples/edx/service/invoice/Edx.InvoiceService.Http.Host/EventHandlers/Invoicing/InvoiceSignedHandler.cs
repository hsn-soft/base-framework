using Edx.InvoiceService.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.Invoicing;

/// <summary>The signer service signed an approved invoice: this service moves the record to signed and drops the draft. It runs as the person who approved.</summary>
public sealed class InvoiceSignedHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceSignedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceSignedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnSignedAsync(envelope.Message, cancellationToken);
    }
}
