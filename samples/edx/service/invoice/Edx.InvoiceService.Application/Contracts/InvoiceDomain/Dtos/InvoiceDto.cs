using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;

/// <summary>
/// An invoice as the caller may see it: one record of an invoice (it is sent again after a failure, and each time is a record). What the caller was not granted the operation to see is left out: the tax ids (<c>operation.invoice.invoice.tax-id.view</c>) and the
/// amounts, the prices of the lines and the total (<c>operation.invoice.invoice.amounts.view</c>), which are then <see langword="null"/>. The header and the lines are empty while the invoice is processed or when it failed before its draft.
/// </summary>
public sealed class InvoiceDto : AuditedEntityDto<Guid>
{
    /// <summary>The uuid of the invoice, as the sender gave it; it repeats when the invoice is sent again.</summary>
    public Guid InvoiceId { get; init; }

    public Guid QueueId { get; init; }

    public string Number { get; init; } = string.Empty;

    /// <summary><c>Model</c>, <c>Xml</c>, <c>Txt</c>, <c>Csv</c> or <c>Xls</c>.</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary><c>ServiceDto</c>, <c>ServiceUploadFile</c>, <c>BackOfficeCreatePage</c> or <c>BackOfficeUploadPage</c>.</summary>
    public string ChannelType { get; init; } = string.Empty;

    /// <summary><c>Processing</c>, <c>Failed</c>, <c>DraftCreated</c>, <c>Rejected</c>, <c>Approved</c> or <c>Signed</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Whether this is the record the invoice is at (a failed or a rejected one is not).</summary>
    public bool IsLive { get; init; }

    public string? FailureStage { get; init; }

    public string? FailureReason { get; init; }

    public DateTimeOffset StatusChangedAt { get; init; }

    public DateTime? IssueDate { get; init; }

    public string? Currency { get; init; }

    public string? SupplierName { get; init; }

    public string? SupplierTaxId { get; init; }

    public string? CustomerName { get; init; }

    public string? CustomerTaxId { get; init; }

    public IReadOnlyList<InvoiceLineDto> Lines { get; init; } = [];

    public decimal? Total { get; init; }

    public DateTimeOffset? ApprovedAt { get; init; }

    public Guid? ApprovedBy { get; init; }

    public DateTimeOffset? RejectedAt { get; init; }

    public Guid? RejectedBy { get; init; }

    public string? RejectionReason { get; init; }

    public DateTimeOffset? SignedAt { get; init; }

    public Guid TenantId { get; init; }
}

public sealed class InvoiceLineDto
{
    public string Description { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal VatRate { get; init; }

    public decimal? Total { get; init; }
}

/// <summary>The XML of an invoice (its draft, or the signed one) and the name it is saved under.</summary>
public sealed class InvoiceXmlDto
{
    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];
}
