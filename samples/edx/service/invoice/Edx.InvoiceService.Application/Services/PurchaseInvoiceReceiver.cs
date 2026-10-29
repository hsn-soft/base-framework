using System.Xml.Linq;
using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.InvoiceService.Application.Services;

/// <summary>
/// An invoice for a tenant came in at its post box (<see cref="PurchaseInvoiceReceivedEto"/>, as the tenant): the XML is copied from the container of the integration service into this service's, what the invoice says of itself is read from it, and the purchase invoice is kept. The
/// integration service checked the envelope and the schema; this service only has to be able to read it. An event that comes twice keeps one invoice (the uuid is unique in a tenant).
/// </summary>
public sealed class PurchaseInvoiceReceiver(IPurchaseInvoiceRepository invoices, IBlobStore blobs, ICurrentTenant tenant, TimeProvider time, IOptions<InvoiceStorageOptions> storage, ILogger<PurchaseInvoiceReceiver> logger)
{
    public async Task OnReceivedAsync(PurchaseInvoiceReceivedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("A purchase invoice is kept by tenant: the event has no tenant.");
        // the invoices came in a zip (the envelope) in the container of the GİB invoice integration: the invoice is taken out of it by its uuid
        Stream source = await blobs.OpenReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"The envelope '{message.ObjectName}' of purchase invoice {message.InvoiceUuid} is not in the store of the GİB invoice integration.");
        byte[] envelope;
        await using (source.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await source.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            envelope = copy.ToArray();
        }

        XElement document = EnvelopeReader.ReadZip(envelope).Elements.FirstOrDefault(element => string.Equals(UblDocument.Uuid(element), message.InvoiceUuid.ToString(), StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException($"Invoice {message.InvoiceUuid} is not in envelope '{message.ObjectName}'.");
        byte[] xml = UblXml.ToBytes(document);
        UblInvoiceSummary summary = UblInvoiceReader.Read(document);
        string path = $"{tenantId:N}/purchase/{summary.IssueDate:yyyy}/{summary.IssueDate:MM}/purchase_{summary.Uuid:N}.xml";
        using (MemoryStream stream = new(xml, writable: false))
        {
            await blobs.SaveAsync(storage.Value.Container, path, stream, "application/xml", cancellationToken).ConfigureAwait(false);
        }

        PurchaseInvoice invoice = PurchaseInvoice.Receive(
            summary.Uuid,
            message.EnvelopeId,
            summary.Number,
            summary.IssueDate,
            summary.Currency,
            summary.SupplierRegisterNumber,
            summary.SupplierName,
            summary.CustomerRegisterNumber,
            summary.CustomerName,
            summary.Lines.Count,
            summary.TaxExclusiveAmount,
            summary.TaxInclusiveAmount,
            summary.PayableAmount,
            path,
            message.ReceivedAt == default ? time.GetUtcNow() : message.ReceivedAt,
            EfaturaStatus.Processed,
            EfaturaStatus.NameOf(EfaturaStatus.Processed),
            summary.ProfileId);
        if (await invoices.TryAddAsync(invoice, cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Purchase invoice {InvoiceUuid} ({InvoiceNumber}) from {SupplierRegisterNumber} was received", summary.Uuid, summary.Number, summary.SupplierRegisterNumber);
        }
        else
        {
            logger.LogInformation("Purchase invoice {InvoiceUuid} was told twice: it is kept once", summary.Uuid);
        }
    }

    /// <summary>The center has a newer status of the envelope the invoices came in: the invoices of that envelope carry it.</summary>
    public async Task OnEnvelopeStatusAsync(PurchaseEnvelopeStatusEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        foreach (PurchaseInvoice invoice in await invoices.FindByEnvelopeAsync(message.EnvelopeId, cancellationToken).ConfigureAwait(false))
        {
            if (invoice.ApplyEnvelopeStatus(message.StatusCode, message.StatusText, message.At))
            {
                await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("The envelope {EnvelopeId} of purchase invoices is {StatusCode} at the center", message.EnvelopeId, message.StatusCode);
    }

    /// <summary>The signer signed the answer to a purchase invoice: the answer is kept with the invoice (and the unsigned document is taken away). The GİB invoice integration takes the same event and sends the answer.</summary>
    public async Task OnAnswerSignedAsync(PurchaseAnswerSignedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        PurchaseInvoice? invoice = await invoices.FindByUuidAsync(message.InvoiceUuid, cancellationToken).ConfigureAwait(false);
        if (invoice is null || invoice.Responses.Any(response => response.DocumentUuid == message.AnswerUuid))
        {
            logger.LogWarning("The answer {AnswerUuid} to purchase invoice {InvoiceUuid} was said signed, and the invoice is not there or has it already", message.AnswerUuid, message.InvoiceUuid);
            return;
        }

        DateTimeOffset now = time.GetUtcNow();
        invoice.AddResponse(InvoiceResponse.Of(Enum.Parse<InvoiceResponseKind>(message.Kind), message.AnswerNumber, message.AnswerUuid, now, message.Note), now);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await blobs.DeleteAsync(storage.Value.Container, InvoicePaths.PurchaseAnswerDraft(invoice.TenantId, invoice.InvoiceUuid), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The answer ({AnswerKind}) to purchase invoice {InvoiceUuid} ({InvoiceNumber}) was signed", message.Kind, invoice.InvoiceUuid, invoice.Number);
    }

    /// <summary>The envelope of the answer said something (the center took it, it was delivered to the supplier, or it was not): the answer keeps it.</summary>
    public async Task OnAnswerEnvelopeAsync(PurchaseAnswerEnvelopeEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        PurchaseInvoice? invoice = await invoices.FindByUuidAsync(message.InvoiceUuid, cancellationToken).ConfigureAwait(false);
        if (invoice is null || !invoice.ApplyAnswerEnvelope(message.AnswerUuid, message.EnvelopeId, message.State, message.StatusCode, message.StatusText, message.At))
        {
            logger.LogWarning("The envelope {EnvelopeId} of the answer {AnswerUuid} said {State}, and the invoice has no such answer", message.EnvelopeId, message.AnswerUuid, message.State);
            return;
        }

        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
    }
}
