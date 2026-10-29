using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Filters;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Submits;
using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base.Application.Dtos;

namespace Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;

/// <summary>
/// What the service does, whatever the way a customer reaches it (REST, gRPC, WCF, or a page of the back office): an invoice is checked at once, kept in the blob store, put in the queue and announced to the invoice service. The answer is that it was queued; what became of it is read
/// from the queue (by the id of the queue item, or by the uuid of the invoice).
/// </summary>
public interface IFirmInvoiceAppService
{
    /// <summary>Queues a zip with one file of the type given. Refused at once when the hash is not the zip's, the zip is not a zip with one file of that type, or the sender may not send this much.</summary>
    Task<QueueItemDto> QueueFileAsync(InvoiceUploadDto input, Stream content, InvoiceChannelType channel, CancellationToken cancellationToken = default);

    /// <summary>Queues the model of an invoice, kept as a JSON or an XML file.</summary>
    Task<QueueItemDto> QueueModelAsync(InvoiceSubmissionDto model, ModelFormat format, InvoiceChannelType channel, CancellationToken cancellationToken = default);

    Task<QueueItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>What is in the queue of an invoice: when one of its items got through (a draft was made, or it is completed) that one, otherwise all of them, the newest first (the ones that failed or were rejected).</summary>
    Task<List<QueueItemDto>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<QueueItemDto>> GetPagedListAsync(GetQueueItemsPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>The bytes of the queued file as they were received, with its name and content type.</summary>
    Task<StoredFileContent> OpenAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>A file read back from the store: the caller disposes the stream.</summary>
public sealed record StoredFileContent(Stream Content, string FileName, string ContentType);
