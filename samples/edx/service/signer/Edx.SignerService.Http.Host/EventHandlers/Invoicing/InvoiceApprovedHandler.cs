using Edx.Shared.Contracts.Events.Invoicing;
using Edx.SignerService.Http.Host.Signing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.SignerService.Http.Host.EventHandlers.Invoicing;

/// <summary>A person approved a draft in the invoice service: this service signs it.</summary>
public sealed class InvoiceApprovedHandler(InvoiceSigningService signing) : IIntegrationEventHandler<InvoiceApprovedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceApprovedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return signing.SignAsync(envelope.Message, cancellationToken);
    }
}
