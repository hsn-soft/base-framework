using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos;

public sealed class ReceivedFileDto : CreationAuditedEntityDto<Guid>
{
    /// <summary>The id of the file in the intake service.</summary>
    public Guid FileId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public string Channel { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public string Container { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public string FolderPath { get; init; } = string.Empty;

    public int EntryCount { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }

    public Guid TenantId { get; init; }
}
