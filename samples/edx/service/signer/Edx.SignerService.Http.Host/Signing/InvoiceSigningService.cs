using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.EventBus;

namespace Edx.SignerService.Http.Host.Signing;

/// <summary>
/// Signs an approved invoice: reads the draft the event names, signs it as the supplier, checks that what goes to the center still follows the UBL-TR schema (signature and all: a signer that makes a document the center refuses is a failure of this service, not of the invoice), writes it in the signed place of the
/// same container and publishes <see cref="InvoiceSignedEto"/>. The work is the same when an event comes twice (the signed object is overwritten by the same bytes, up to the signing time), and a failure is thrown, so the bus tries again.
/// </summary>
public sealed class InvoiceSigningService(IBlobStore blobs, IInvoiceSigner signer, IEventBus eventBus, ILogger<InvoiceSigningService> logger)
{
    private const string XmlContentType = "application/xml";
    private const int MaxProblemsInReason = 3;

    public async Task SignAsync(InvoiceApprovedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        byte[] draft = await ReadAsync(message.Container, message.DraftPath, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The draft '{message.DraftPath}' of invoice {message.InvoiceId} is not in the store.");
        byte[] signed = signer.Sign(draft, message.SignerName);

        IReadOnlyList<string> problems = UblSchemas.Default.Validate(UblXml.Parse(signed));
        if (problems.Count > 0)
        {
            throw new InvalidOperationException($"The signed invoice {message.InvoiceId} does not follow the UBL-TR schema: {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
        }

        // where it is kept is the invoice service's to say (it is in the event)
        string signedPath = message.SignedPath;
        using MemoryStream stream = new(signed, writable: false);
        await blobs.SaveAsync(message.Container, signedPath, stream, XmlContentType, cancellationToken).ConfigureAwait(false);

        await eventBus.PublishAsync(
            new InvoiceSignedEto(message.RecordId, message.QueueId, message.InvoiceId, message.InvoiceNumber, message.CustomerRegisterNumber, message.CustomerName, message.Container, signedPath),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was signed", message.InvoiceId, message.InvoiceNumber);
    }

    /// <summary>
    /// Signs the answer to a commercial invoice (an application response) the same way: reads the unsigned document, signs it as the tenant that answers, checks it follows the UBL-TR schema, writes it where the event says and publishes <see cref="PurchaseAnswerSignedEto"/>.
    /// </summary>
    public async Task SignAnswerAsync(PurchaseAnswerRequestedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        byte[] draft = await ReadAsync(message.Container, message.DraftPath, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The answer '{message.DraftPath}' to invoice {message.InvoiceUuid} is not in the store.");
        byte[] signed = signer.Sign(draft, message.SignerName);

        IReadOnlyList<string> problems = UblSchemas.Default.Validate(UblXml.Parse(signed));
        if (problems.Count > 0)
        {
            throw new InvalidOperationException($"The signed answer to invoice {message.InvoiceUuid} does not follow the UBL-TR schema: {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
        }

        using MemoryStream stream = new(signed, writable: false);
        await blobs.SaveAsync(message.Container, message.SignedPath, stream, XmlContentType, cancellationToken).ConfigureAwait(false);

        await eventBus.PublishAsync(
            new PurchaseAnswerSignedEto(
                message.InvoiceUuid, message.InvoiceNumber, message.AnswerUuid, message.AnswerNumber, message.Kind, message.Note, message.SupplierRegisterNumber, message.SupplierName, message.InvoiceEnvelopeId, message.Container, message.SignedPath),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The answer ({AnswerKind}) to invoice {InvoiceUuid} ({InvoiceNumber}) was signed", message.Kind, message.InvoiceUuid, message.InvoiceNumber);
    }

    private async Task<byte[]?> ReadAsync(string container, string path, CancellationToken cancellationToken)
    {
        Stream? stream = await blobs.OpenReadAsync(container, path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            return copy.ToArray();
        }
    }
}
