using HsnSoft.Base.Application.Dtos;

namespace Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;

/// <summary>An invoice of the queue as the caller may see it: where it is, and what became of it. Where its file is kept is left empty for whoever was not granted <c>operation.firm-invoice.file.storage-location.view</c>.</summary>
public sealed class QueueItemDto : AuditedEntityDto<Guid>
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    /// <summary>The host of the firm invoice integration that received it: <c>Edx.Integration.FirmInvoice.Http</c>, <c>.Grpc</c> or <c>.Wcf</c>.</summary>
    public string ReceivedBy { get; init; } = string.Empty;

    /// <summary><c>Model</c>, <c>Xml</c>, <c>Txt</c>, <c>Csv</c> or <c>Xls</c>.</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary><c>ServiceDto</c>, <c>ServiceUploadFile</c>, <c>BackOfficeCreatePage</c> or <c>BackOfficeUploadPage</c>.</summary>
    public string ChannelType { get; init; } = string.Empty;

    /// <summary><c>Queued</c>, <c>DraftCreated</c>, <c>Rejected</c>, <c>Failed</c> or <c>Completed</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Why it failed or was rejected.</summary>
    public string? StatusReason { get; init; }

    public DateTimeOffset StatusChangedAt { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string FileHash { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public string? EntryName { get; init; }

    public string FolderPath { get; init; } = string.Empty;

    public string Container { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public Guid TenantId { get; init; }
}
