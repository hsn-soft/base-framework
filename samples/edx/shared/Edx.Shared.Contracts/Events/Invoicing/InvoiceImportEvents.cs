using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Invoicing;

/// <summary>The invoice service could not take an invoice in (it is being processed already, it exists, the zip could not be opened, the file does not follow its schema, the sender may not make one this large, ...). The intake service marks the queue item failed, with the reason.</summary>
/// <param name="QueueId">The queue item.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
/// <param name="Stage">Where it went wrong: <c>Claim</c>, <c>Fetch</c>, <c>Extract</c>, <c>Validate</c>, <c>Limit</c> or <c>Convert</c>.</param>
/// <param name="Reason">What is wrong, for the person who sent it.</param>
public sealed record InvoiceImportFailedEto(Guid QueueId, Guid InvoiceId, string Stage, string Reason) : IIntegrationEventMessage;

/// <summary>The draft of the invoice was made and waits for a person to approve or reject it. The queue item is marked so.</summary>
public sealed record InvoiceDraftCreatedEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>A person rejected the draft. The queue item is marked so, with the reason; the sender may send the invoice again with the same number.</summary>
public sealed record InvoiceRejectedEto(Guid QueueId, Guid InvoiceId, string Reason) : IIntegrationEventMessage;

/// <summary>The invoice was approved, signed and kept in its signed place: nothing is left to do. The queue item is marked completed.</summary>
public sealed record InvoiceCompletedEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>A failed or rejected invoice was replaced by a correct one that came later: the old queue item is deleted (softly).</summary>
public sealed record InvoiceSupersededEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>
/// A person approved the draft: the invoice is to be signed. It is the invoice service's own event (it tells itself, so the signing runs apart from the request of the person, as the person: the envelope carries who approved).
/// </summary>
/// <param name="RecordId">The id of the invoice record (its primary key).</param>
/// <param name="QueueId">The queue item.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
public sealed record InvoiceApprovedEto(Guid RecordId, Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;
