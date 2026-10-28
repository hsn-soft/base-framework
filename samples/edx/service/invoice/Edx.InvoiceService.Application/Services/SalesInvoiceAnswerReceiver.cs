using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using HsnSoft.Base.BlobStoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.InvoiceService.Application.Services;

/// <summary>
/// An answer to a commercial invoice of the tenant came in at its sender unit (the receiver accepted or refused the invoice): the answer is taken out of the zip of the envelope (the only file in the container of the GİB invoice integration), kept beside the invoice
/// (<c>{tenant}/sales/{year}/{month}/sales_answer_{ettn}.xml</c>) and put in the invoice's <c>Responses</c>.
/// </summary>
public sealed class SalesInvoiceAnswerReceiver(ISalesInvoiceRepository invoices, IBlobStore blobs, TimeProvider time, IOptions<InvoiceStorageOptions> storage, ILogger<SalesInvoiceAnswerReceiver> logger)
{
    public async Task OnReceivedAsync(SalesInvoiceAnswerReceivedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SalesInvoice? invoice = message.InvoiceUuid == Guid.Empty ? null : await invoices.FindLiveAsync(message.InvoiceUuid, cancellationToken).ConfigureAwait(false);
        if (invoice is not { Status: SalesInvoiceStatus.Signed, IssueDate: { } issued } || invoice.Profile != UblInvoiceBuilder.CommercialProfileId)
        {
            logger.LogWarning("The answer {AnswerNumber} to invoice {InvoiceNumber} ({InvoiceUuid}) came in and the tenant has no such signed commercial invoice: it is left", message.AnswerNumber, message.InvoiceNumber, message.InvoiceUuid);
            return;
        }

        if (invoice.Responses.Any(response => response.DocumentUuid == message.AnswerUuid))
        {
            return;
        }

        Stream source = await blobs.OpenReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"The envelope '{message.ObjectName}' of the answer {message.AnswerUuid} is not in the store of the GİB invoice integration.");
        byte[] zip;
        await using (source.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await source.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            zip = copy.ToArray();
        }

        System.Xml.Linq.XElement answer = EnvelopeReader.ReadZip(zip).Elements.FirstOrDefault(element => string.Equals(UblDocument.Uuid(element), message.AnswerUuid.ToString(), StringComparison.OrdinalIgnoreCase))
                                          ?? throw new InvalidOperationException($"The answer {message.AnswerUuid} is not in envelope '{message.ObjectName}'.");
        using (MemoryStream stream = new(UblXml.ToBytes(answer), writable: false))
        {
            await blobs.SaveAsync(storage.Value.Container, InvoicePaths.SalesAnswer(invoice.TenantId, issued, invoice.InvoiceId), stream, "application/xml", cancellationToken).ConfigureAwait(false);
        }

        DateTimeOffset now = time.GetUtcNow();
        invoice.AddResponse(InvoiceResponse.Of(Enum.Parse<InvoiceResponseKind>(message.Kind), message.AnswerNumber, message.AnswerUuid, message.ReceivedAt == default ? now : message.ReceivedAt, message.Note, message.EnvelopeId), now);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was answered by its receiver: {AnswerKind}", invoice.InvoiceId, invoice.Number, message.Kind);
    }
}
