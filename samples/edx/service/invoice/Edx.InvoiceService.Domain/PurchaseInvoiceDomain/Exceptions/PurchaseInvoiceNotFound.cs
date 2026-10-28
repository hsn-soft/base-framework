using HsnSoft.Base;

namespace Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Exceptions;

/// <summary>There is no such purchase invoice (or it is another tenant's): answered as 404.</summary>
public static class PurchaseInvoiceNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Purchase invoice {id} was not found.").WithKey("purchase_invoice_not_found", id);
}
