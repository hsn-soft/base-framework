using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.InvoiceService.Domain.InvoiceReportDomain.Repositories;

/// <summary>The fields a list of reports may be sorted by and the sorting when a client sends none: the newest first.</summary>
public static class InvoiceReportSorting
{
    public const string Default = "generatedAt desc";

    public static readonly SortFields<InvoiceReport> Fields = new SortFields<InvoiceReport>()
        .Add("generatedAt", report => report.GeneratedAt)
        .Add("periodStart", report => report.PeriodStart)
        .Add("totalAmount", report => report.TotalAmount)
        .Add("invoiceCount", report => report.InvoiceCount);
}
