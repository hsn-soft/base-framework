using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;

public interface IPurchaseInvoiceRepository : IRepository<PurchaseInvoice, Guid>
{
    /// <summary>Adds the purchase invoice only when the tenant has none of that uuid: the store decides (a unique index), so an event that comes twice keeps one. <see langword="false"/> means it was there.</summary>
    Task<bool> TryAddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>The purchase invoice of the tenant with this uuid (its ETTN), when there is one.</summary>
    Task<PurchaseInvoice?> FindByUuidAsync(Guid invoiceUuid, CancellationToken cancellationToken = default);

    /// <summary>The invoices that came in an envelope (all for one post box, so for one tenant).</summary>
    Task<IReadOnlyList<PurchaseInvoice>> FindByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default);
}
