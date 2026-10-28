using Edx.InvoiceService.Application.Services;
using Edx.Shared.Contracts.Events.Integration;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.Integration;

/// <summary>The center took the envelope of the invoice: the invoice is sent. It runs as the tenant of the invoice (the envelope says which).</summary>
public sealed class InvoiceSentHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceSentEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceSentEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnSentAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The receiver's post box took the invoice in: the invoice is delivered, and the queue is told it is completed.</summary>
public sealed class InvoiceDeliveredHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceDeliveredEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceDeliveredEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnDeliveredAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The invoice did not reach its receiver: the invoice says where and why, and the queue is told it failed.</summary>
public sealed class InvoiceDeliveryFailedHandler(InvoiceImportService import) : IIntegrationEventHandler<InvoiceDeliveryFailedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceDeliveryFailedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return import.OnDeliveryFailedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>An invoice for a tenant came in at its post box: the purchase invoice is kept. It runs as the tenant the invoice is for.</summary>
public sealed class PurchaseInvoiceReceivedHandler(PurchaseInvoiceReceiver receiver) : IIntegrationEventHandler<PurchaseInvoiceReceivedEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseInvoiceReceivedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receiver.OnReceivedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The center has a newer status of an envelope that came in: the purchase invoices of that envelope carry it. It runs as the tenant the envelope is for.</summary>
public sealed class PurchaseEnvelopeStatusHandler(PurchaseInvoiceReceiver receiver) : IIntegrationEventHandler<PurchaseEnvelopeStatusEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseEnvelopeStatusEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receiver.OnEnvelopeStatusAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The envelope of the answer to a purchase invoice said something: the answer keeps it. It runs as the tenant that answered.</summary>
public sealed class PurchaseAnswerEnvelopeHandler(PurchaseInvoiceReceiver receiver) : IIntegrationEventHandler<PurchaseAnswerEnvelopeEto>
{
    public Task HandleAsync(MessageEnvelope<PurchaseAnswerEnvelopeEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receiver.OnAnswerEnvelopeAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>An answer to a commercial invoice of the tenant came in: it is kept with the sales invoice. It runs as the tenant the answer is for.</summary>
public sealed class SalesInvoiceAnswerReceivedHandler(SalesInvoiceAnswerReceiver receiver) : IIntegrationEventHandler<SalesInvoiceAnswerReceivedEto>
{
    public Task HandleAsync(MessageEnvelope<SalesInvoiceAnswerReceivedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receiver.OnReceivedAsync(envelope.Message, cancellationToken);
    }
}
