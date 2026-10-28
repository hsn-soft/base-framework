using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Integration;

/// <summary>The envelope of the invoice was made and the center took it (<c>sendDocument</c> answered): the invoice is on its way. The invoice record is marked sent.</summary>
/// <param name="RecordId">The invoice record.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
/// <param name="EnvelopeId">The uuid of the envelope.</param>
public sealed record InvoiceSentEto(Guid RecordId, Guid InvoiceId, Guid EnvelopeId) : IIntegrationEventMessage;

/// <summary>The invoice reached its receiver: the center says the receiver's post box took the envelope in (1300). The invoice record is marked delivered and the queue item completed.</summary>
public sealed record InvoiceDeliveredEto(Guid RecordId, Guid InvoiceId, Guid EnvelopeId, int StatusCode) : IIntegrationEventMessage;

/// <summary>
/// The invoice did not reach its receiver. The invoice record is marked so, and the queue item failed, with the reason; the same invoice may be sent again (with a new envelope, and the same uuid and number, which the center lets be sent again after its own failures).
/// </summary>
/// <param name="RecordId">The invoice record.</param>
/// <param name="InvoiceId">The uuid of the invoice.</param>
/// <param name="EnvelopeId">The uuid of the envelope, when one was made.</param>
/// <param name="Stage">Where it went wrong: <c>Receiver</c> (it is not an e-invoice user), <c>Send</c> (the center could not be reached or refused the call), <c>Center</c> (the center refused the envelope) or <c>Delivery</c> (the center could not deliver it, or the post box refused it).</param>
/// <param name="StatusCode">The code of the center, when there is one (0 when there is none).</param>
/// <param name="Reason">What is wrong, for the person who sent the invoice.</param>
public sealed record InvoiceDeliveryFailedEto(Guid RecordId, Guid InvoiceId, Guid? EnvelopeId, string Stage, int StatusCode, string Reason) : IIntegrationEventMessage;

/// <summary>
/// The GİB invoice integration asked the center for the status of an envelope that came in at a post box and got a newer one (1220 while the center waits for our answer, 1300 once it has it, a failure code otherwise). The invoice service applies it to the purchase invoices that came in the envelope.
/// </summary>
public sealed record PurchaseEnvelopeStatusEto(Guid EnvelopeId, int StatusCode, string StatusText, DateTimeOffset At) : IIntegrationEventMessage;

/// <summary>
/// An invoice for a tenant came in at its post box, was checked (the envelope, the UBL-TR schema, the receiver) and was taken (1200): the invoice service keeps it as a purchase invoice of the tenant. Published as the tenant (the envelope of the event says which).
/// </summary>
/// <param name="EnvelopeId">The uuid of the envelope it came in.</param>
/// <param name="InvoiceUuid">The uuid of the invoice (its ETTN).</param>
/// <param name="InvoiceNumber">The number of the invoice.</param>
/// <param name="SupplierRegisterNumber">The VKN or TCKN of the one who issued it.</param>
/// <param name="SupplierName">Its title.</param>
/// <param name="Container">The container of the integration service's blob store.</param>
/// <param name="ObjectName">The XML of the invoice in the container.</param>
/// <param name="ReceivedAt">When the post box took it in.</param>
public sealed record PurchaseInvoiceReceivedEto(Guid EnvelopeId, Guid InvoiceUuid, string InvoiceNumber, string SupplierRegisterNumber, string SupplierName, string Container, string ObjectName, DateTimeOffset ReceivedAt) : IIntegrationEventMessage;

/// <summary>
/// Where the envelope of an answer to a commercial invoice is (the GİB invoice integration sent it to the center for the tenant that answered): <c>Sent</c> when the center took it, <c>Delivered</c> (1300) when it was placed at the supplier's system, <c>Refused</c> or <c>Failed</c> when it was not. The invoice service puts it on the answer kept with the purchase invoice.
/// </summary>
public sealed record PurchaseAnswerEnvelopeEto(Guid InvoiceUuid, Guid AnswerUuid, Guid EnvelopeId, string State, int StatusCode, string StatusText, string? Note, DateTimeOffset At) : IIntegrationEventMessage;

/// <summary>
/// An answer to a commercial invoice of the tenant came in at its sender unit (the receiver accepted or refused the invoice): the invoice service keeps the answer with the sales invoice. The answer is in the zip of the envelope, in the container of the GİB invoice integration.
/// </summary>
/// <param name="EnvelopeId">The uuid of the envelope the answer came in.</param>
/// <param name="InvoiceNumber">The number of the invoice that was answered.</param>
/// <param name="InvoiceUuid">The uuid of that invoice (empty when the answer does not say it).</param>
/// <param name="Kind"><c>Accepted</c> or <c>Rejected</c>.</param>
/// <param name="AnswerNumber">The number of the answer document.</param>
/// <param name="AnswerUuid">The uuid of the answer document.</param>
/// <param name="Note">What the receiver said.</param>
/// <param name="Container">The container of the integration's blob store.</param>
/// <param name="ObjectName">The zip of the envelope.</param>
/// <param name="ReceivedAt">When the sender unit took the envelope in.</param>
public sealed record SalesInvoiceAnswerReceivedEto(Guid EnvelopeId, string InvoiceNumber, Guid InvoiceUuid, string Kind, string AnswerNumber, Guid AnswerUuid, string? Note, string Container, string ObjectName, DateTimeOffset ReceivedAt) : IIntegrationEventMessage;
