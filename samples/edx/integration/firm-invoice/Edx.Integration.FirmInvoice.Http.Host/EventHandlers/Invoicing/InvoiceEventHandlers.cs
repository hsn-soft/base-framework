using Edx.Integration.FirmInvoice.Application.Services;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.Integration.FirmInvoice.Http.Host.EventHandlers.Invoicing;

/// <summary>The invoice service could not take an invoice in: the queue item is marked failed, with where and why. It runs as the user who sent the invoice, in its tenant (the envelope says who).</summary>
public sealed class InvoiceImportFailedHandler(QueueUpdater queue) : IIntegrationEventHandler<InvoiceImportFailedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceImportFailedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return queue.OnFailedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The draft was made: the queue item says it waits for approval.</summary>
public sealed class InvoiceDraftCreatedHandler(QueueUpdater queue) : IIntegrationEventHandler<InvoiceDraftCreatedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceDraftCreatedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return queue.OnDraftCreatedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>A person rejected the draft: the queue item says so.</summary>
public sealed class InvoiceRejectedHandler(QueueUpdater queue) : IIntegrationEventHandler<InvoiceRejectedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceRejectedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return queue.OnRejectedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>The invoice was approved and signed: the queue item is completed.</summary>
public sealed class InvoiceCompletedHandler(QueueUpdater queue) : IIntegrationEventHandler<InvoiceCompletedEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceCompletedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return queue.OnCompletedAsync(envelope.Message, cancellationToken);
    }
}

/// <summary>A correct invoice replaced a failed or rejected one: the old queue item is deleted.</summary>
public sealed class InvoiceSupersededHandler(QueueUpdater queue) : IIntegrationEventHandler<InvoiceSupersededEto>
{
    public Task HandleAsync(MessageEnvelope<InvoiceSupersededEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return queue.OnSupersededAsync(envelope.Message, cancellationToken);
    }
}
