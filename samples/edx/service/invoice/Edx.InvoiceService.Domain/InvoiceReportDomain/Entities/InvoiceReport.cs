using Edx.InvoiceService.Domain.InvoiceReportDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;

/// <summary>
/// A report of the invoices of a tenant in a period, in one currency: how many there were, how many were approved and what they amounted to. It is made on request and kept as it was made (a snapshot: the invoices of that day, not of today). The audit fields (who
/// made it) and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class InvoiceReport : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private InvoiceReport()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public DateTime PeriodStart { get; private set; }

    public DateTime PeriodEnd { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public int InvoiceCount { get; private set; }

    public int ApprovedCount { get; private set; }

    /// <summary>What the invoices of the period amount to, with VAT.</summary>
    public decimal TotalAmount { get; private set; }

    public DateTimeOffset GeneratedAt { get; private set; }

    public Guid TenantId { get; private set; }

    public static InvoiceReport Generate(DateTime periodStart, DateTime periodEnd, string currency, int invoiceCount, int approvedCount, decimal totalAmount, DateTimeOffset generatedAt)
    {
        if (periodEnd.Date < periodStart.Date)
        {
            throw new DomainException("The period ends before it starts.").WithKey("report_period_invalid");
        }

        string code = Guard.Length(Guard.NotNullOrWhiteSpace(currency, nameof(Currency), 3).ToUpperInvariant(), nameof(Currency), 3, 3)!;
        return new InvoiceReport
        {
            Id = Guid.CreateVersion7(),
            Name = Guard.Length($"{periodStart:yyyy-MM-dd} - {periodEnd:yyyy-MM-dd} ({code})", nameof(Name), InvoiceReportConsts.NameMaxLength)!,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEnd.Date,
            Currency = code,
            InvoiceCount = Guard.NotNegative(invoiceCount, nameof(InvoiceCount)),
            ApprovedCount = Guard.NotNegative(approvedCount, nameof(ApprovedCount)),
            TotalAmount = Guard.NotNegative(totalAmount, nameof(TotalAmount)),
            GeneratedAt = generatedAt,
        };
    }
}
