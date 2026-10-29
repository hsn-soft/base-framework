using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos;

/// <summary>A report as the caller may see it: what it amounts to is <see langword="null"/> for whoever was not granted <c>operation.invoice-report.total.view</c>.</summary>
public sealed class InvoiceReportDto : CreationAuditedEntityDto<Guid>
{
    public string Name { get; init; } = string.Empty;

    public DateTime PeriodStart { get; init; }

    public DateTime PeriodEnd { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int InvoiceCount { get; init; }

    public int ApprovedCount { get; init; }

    public decimal? TotalAmount { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public Guid TenantId { get; init; }
}
