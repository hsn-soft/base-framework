using Edx.InvoiceService.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.Invoicing;

/// <summary>A person approved a draft (this service told itself): the invoice is signed and kept, and the intake service is told it is completed. It runs as the person who approved.</summary>
public sealed class InvoiceApprovedHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceApprovedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceApprovedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnApprovedAsync(envelope.Message, cancellationToken);
    }
}
