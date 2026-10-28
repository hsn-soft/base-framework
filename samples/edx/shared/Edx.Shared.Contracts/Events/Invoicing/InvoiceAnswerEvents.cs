using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Invoicing;

/// <summary>
/// A person of the receiving tenant answered a commercial invoice that came in (accepted or refused it): the invoice service made the application response document and asks the signer service to sign it. It runs as the person who answered.
/// </summary>
/// <param name="InvoiceUuid">The uuid of the invoice that is answered (its ETTN).</param>
/// <param name="InvoiceNumber">The number of the invoice.</param>
/// <param name="AnswerUuid">The uuid of the answer document.</param>
/// <param name="AnswerNumber">The number of the answer document.</param>
/// <param name="Kind"><c>Accepted</c> or <c>Rejected</c>.</param>
/// <param name="Note">What the person said (required when the invoice is refused).</param>
/// <param name="SupplierRegisterNumber">The VKN or TCKN of the supplier: the one the answer goes to.</param>
/// <param name="SupplierName">The title of the supplier.</param>
/// <param name="InvoiceEnvelopeId">The uuid of the envelope the invoice came in (the answer goes back to the address it came from).</param>
/// <param name="SignerName">Who signs: the tenant that answers.</param>
/// <param name="Container">The container of the invoice service's blob store.</param>
/// <param name="DraftPath">The answer document (unsigned) in the container.</param>
/// <param name="SignedPath">Where the signed answer is to be written in the container.</param>
public sealed record PurchaseAnswerRequestedEto(
    Guid InvoiceUuid,
    string InvoiceNumber,
    Guid AnswerUuid,
    string AnswerNumber,
    string Kind,
    string? Note,
    string SupplierRegisterNumber,
    string SupplierName,
    Guid InvoiceEnvelopeId,
    string SignerName,
    string Container,
    string DraftPath,
    string SignedPath) : IIntegrationEventMessage;

/// <summary>The signer service signed the answer and wrote it in the signed place of the invoice service's container: the invoice service keeps the answer with the invoice it is about, and the GİB invoice integration puts it in an envelope and sends it.</summary>
/// <param name="InvoiceUuid">The uuid of the invoice that was answered.</param>
/// <param name="InvoiceNumber">The number of the invoice.</param>
/// <param name="AnswerUuid">The uuid of the answer document.</param>
/// <param name="AnswerNumber">The number of the answer document.</param>
/// <param name="Kind"><c>Accepted</c> or <c>Rejected</c>.</param>
/// <param name="Note">What the person said.</param>
/// <param name="SupplierRegisterNumber">The VKN or TCKN of the supplier.</param>
/// <param name="SupplierName">The title of the supplier.</param>
/// <param name="InvoiceEnvelopeId">The uuid of the envelope the invoice came in.</param>
/// <param name="Container">The container of the invoice service's blob store.</param>
/// <param name="ObjectName">The signed answer in the container.</param>
public sealed record PurchaseAnswerSignedEto(
    Guid InvoiceUuid,
    string InvoiceNumber,
    Guid AnswerUuid,
    string AnswerNumber,
    string Kind,
    string? Note,
    string SupplierRegisterNumber,
    string SupplierName,
    Guid InvoiceEnvelopeId,
    string Container,
    string ObjectName) : IIntegrationEventMessage;
