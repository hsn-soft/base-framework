using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

/// <summary>The invoices in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the add that the unique indexes of the live records decide.</summary>
public sealed class MongoSalesInvoiceRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<SalesInvoice, Guid>(database, concepts, collectionName), ISalesInvoiceRepository
{
    public Task<SalesInvoice?> FindLiveAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => FindAsync(invoice => invoice.InvoiceId == invoiceId && invoice.IsLive, cancellationToken);

    public async Task<bool> TryAddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
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

    public Task<IReadOnlyList<SalesInvoice>> FindReplaceableAsync(Guid invoiceId, Guid exceptId, CancellationToken cancellationToken = default)
        => GetListAsync(
            new ListQueryOptions<SalesInvoice> { Filter = invoice => invoice.InvoiceId == invoiceId && invoice.Id != exceptId && (invoice.Status == SalesInvoiceStatus.Failed || invoice.Status == SalesInvoiceStatus.Rejected) },
            cancellationToken);
}
