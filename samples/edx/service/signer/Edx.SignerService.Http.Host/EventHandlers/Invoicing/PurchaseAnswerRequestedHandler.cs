using Edx.Shared.Contracts.Events.Invoicing;
using Edx.SignerService.Http.Host.Signing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.SignerService.Http.Host.EventHandlers.Invoicing;

/// <summary>A person answered a commercial invoice that came in (in the invoice service): this service signs the answer.</summary>
public sealed class PurchaseAnswerRequestedHandler(InvoiceSigningService signing) : IIntegrationEventHandler<PurchaseAnswerRequestedEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseAnswerRequestedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return signing.SignAnswerAsync(envelope.Message, cancellationToken);
    }
}
