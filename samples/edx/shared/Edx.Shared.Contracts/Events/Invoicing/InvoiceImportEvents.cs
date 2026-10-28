using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Invoicing;

/// <summary>The invoice service could not take an invoice in (it is being processed already, it exists, the zip could not be opened, the file does not follow its schema, the sender may not make one this large, ...). The firm invoice integration marks the queue item failed, with the reason.</summary>
/// <param name="QueueId">The queue item.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
/// <param name="Stage">Where it went wrong: <c>Claim</c>, <c>Fetch</c>, <c>Extract</c>, <c>Validate</c>, <c>Limit</c> or <c>Convert</c>.</param>
/// <param name="Reason">What is wrong, for the person who sent it.</param>
public sealed record InvoiceImportFailedEto(Guid QueueId, Guid InvoiceId, string Stage, string Reason) : IIntegrationEventMessage;

/// <summary>The draft of the invoice was made and waits for a person to approve or reject it. The queue item is marked so.</summary>
public sealed record InvoiceDraftCreatedEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>A person rejected the draft. The queue item is marked so, with the reason; the sender may send the invoice again with the same number.</summary>
public sealed record InvoiceRejectedEto(Guid QueueId, Guid InvoiceId, string Reason) : IIntegrationEventMessage;

/// <summary>The invoice reached its receiver: the center took the envelope and the receiver's post box took it in (the status 1300). Nothing is left to do. The queue item is marked completed.</summary>
public sealed record InvoiceCompletedEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>A failed or rejected invoice was replaced by a correct one that came later: the old queue item is deleted (softly).</summary>
public sealed record InvoiceSupersededEto(Guid QueueId, Guid InvoiceId) : IIntegrationEventMessage;

/// <summary>
/// A person approved the draft: the invoice is to be signed. The invoice service publishes it, the signer service handles it (it reads the draft from the container, signs it and publishes <see cref="InvoiceSignedEto"/>); the envelope carries who approved.
/// </summary>
/// <param name="RecordId">The id of the invoice record (its primary key).</param>
/// <param name="QueueId">The queue item.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
/// <param name="InvoiceNumber">The number of the invoice.</param>
/// <param name="CustomerRegisterNumber">The VKN or TCKN of the customer (the signer only passes it on).</param>
/// <param name="CustomerName">The title of the customer (the signer only passes it on).</param>
/// <param name="SignerName">Who signs: the supplier of the invoice, the tenant's title.</param>
/// <param name="Container">The container of the invoice service's blob store.</param>
/// <param name="DraftPath">The draft XML (UBL-TR, unsigned) in the container.</param>
/// <param name="SignedPath">Where the signed XML is to be written in the container (the invoice service says: <c>{tenant}/sales/{year}/{month}/sales_{ettn}.xml</c>).</param>
public sealed record InvoiceApprovedEto(Guid RecordId, Guid QueueId, Guid InvoiceId, string InvoiceNumber, string CustomerRegisterNumber, string CustomerName, string SignerName, string Container, string DraftPath, string SignedPath) : IIntegrationEventMessage;

/// <summary>
/// The signer service signed the approved invoice (a UBL-TR document with its signature) and wrote it in the signed place of the container of the invoice service. The invoice service moves the invoice to signed, and the GİB invoice integration makes its envelope and hands it to the center. The customer is named by its register number and title; the integration service finds
/// the address of the post box from them.
/// </summary>
/// <param name="RecordId">The id of the invoice record (its primary key).</param>
/// <param name="QueueId">The queue item.</param>
/// <param name="InvoiceId">The uuid of the invoice (its ETTN).</param>
/// <param name="InvoiceNumber">The number of the invoice.</param>
/// <param name="CustomerRegisterNumber">The VKN or TCKN of the customer.</param>
/// <param name="CustomerName">The title of the customer.</param>
/// <param name="Container">The container of the invoice service's blob store.</param>
/// <param name="ObjectName">The signed XML in the container.</param>
public sealed record InvoiceSignedEto(Guid RecordId, Guid QueueId, Guid InvoiceId, string InvoiceNumber, string CustomerRegisterNumber, string CustomerName, string Container, string ObjectName) : IIntegrationEventMessage;
