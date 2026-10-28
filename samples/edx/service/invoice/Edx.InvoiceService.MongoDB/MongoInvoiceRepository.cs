using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

/// <summary>The invoices in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the add that the unique indexes of the live records decide.</summary>
public sealed class MongoInvoiceRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<Invoice, Guid>(database, concepts, collectionName), IInvoiceRepository
{
    public Task<Invoice?> FindLiveAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => FindAsync(invoice => invoice.InvoiceId == invoiceId && invoice.IsLive, cancellationToken);

    public async Task<bool> TryAddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAsync(invoice, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // another record of the invoice (or another invoice with this number) is live: nothing was added
            return false;
        }
    }

    public Task<IReadOnlyList<Invoice>> FindReplaceableAsync(Guid invoiceId, Guid exceptId, CancellationToken cancellationToken = default)
        => GetListAsync(
            new ListQueryOptions<Invoice> { Filter = invoice => invoice.InvoiceId == invoiceId && invoice.Id != exceptId && (invoice.Status == InvoiceStatus.Failed || invoice.Status == InvoiceStatus.Rejected) },
            cancellationToken);
}
