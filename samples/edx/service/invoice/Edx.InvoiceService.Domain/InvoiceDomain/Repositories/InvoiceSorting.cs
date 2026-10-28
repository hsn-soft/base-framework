using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.InvoiceService.Domain.InvoiceDomain.Repositories;

/// <summary>The fields a list of invoices may be sorted by and the sorting when a client sends none: the latest news first.</summary>
public static class InvoiceSorting
{
    public const string Default = "statusChangedAt desc";

    public static readonly SortFields<Invoice> Fields = new SortFields<Invoice>()
        .Add("statusChangedAt", invoice => invoice.StatusChangedAt)
        .Add("issueDate", invoice => invoice.IssueDate)
        .Add("number", invoice => invoice.NormalizedNumber)
        .Add("total", invoice => invoice.Total)
        .Add("status", invoice => invoice.Status)
        .Add("createdAt", invoice => invoice.CreatedAt);
}
