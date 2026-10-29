using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

/// <summary>The purchase invoices in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the add that the unique index of the uuid decides.</summary>
public sealed class MongoPurchaseInvoiceRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<PurchaseInvoice, Guid>(database, concepts, collectionName), IPurchaseInvoiceRepository
{
    public Task<PurchaseInvoice?> FindByUuidAsync(Guid invoiceUuid, CancellationToken cancellationToken = default)
        => FindAsync(invoice => invoice.InvoiceUuid == invoiceUuid, cancellationToken);

    public async Task<IReadOnlyList<PurchaseInvoice>> FindByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        => await GetListAsync(new ListQueryOptions<PurchaseInvoice> { Filter = invoice => invoice.EnvelopeId == envelopeId }, cancellationToken).ConfigureAwait(false);

    public async Task<bool> TryAddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAsync(invoice, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
