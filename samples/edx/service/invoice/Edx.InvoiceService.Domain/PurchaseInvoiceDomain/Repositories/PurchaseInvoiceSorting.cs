using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;

/// <summary>The fields a list of purchase invoices may be sorted by, and the sorting when a client sends none: the latest to come in first.</summary>
public static class PurchaseInvoiceSorting
{
    public const string Default = "receivedAt desc";

    public static readonly SortFields<PurchaseInvoice> Fields = new SortFields<PurchaseInvoice>()
        .Add("receivedAt", invoice => invoice.ReceivedAt)
        .Add("issueDate", invoice => invoice.IssueDate)
        .Add("number", invoice => invoice.NormalizedNumber)
        .Add("supplier", invoice => invoice.SupplierName)
        .Add("payableAmount", invoice => invoice.PayableAmount);
}
