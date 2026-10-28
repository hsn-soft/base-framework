using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.InvoiceService.Domain.InvoiceDomain.Repositories;

/// <summary>Where the invoices are kept: what every repository does, and the two things that have to be one step of the store.</summary>
public interface ISalesInvoiceRepository : IRepository<SalesInvoice, Guid>
{
    /// <summary>The live record of the invoice (see <see cref="SalesInvoice.IsLive"/>) of the tenant, when there is one.</summary>
    Task<SalesInvoice?> FindLiveAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the invoice only when no live record of that uuid, and none of that number, is there: the store decides (a unique index over the live records that are not deleted), so of two that arrive at the same time one is added and the other is not. <see langword="false"/> is
    /// "nothing changed": somebody was first. A record that is not live (a failed one) is always added.
    /// </summary>
    Task<bool> TryAddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>The records of the invoice that a correct one replaces: not deleted, failed or rejected, other than <paramref name="exceptId"/>.</summary>
    Task<IReadOnlyList<SalesInvoice>> FindReplaceableAsync(Guid invoiceId, Guid exceptId, CancellationToken cancellationToken = default);
}
