using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Domain.ProductDomain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB;

/// <summary>The products in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the changes of the stock, each one update of the database (<c>$inc</c> on the document that still has enough).</summary>
public sealed class MongoProductRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<Product, Guid>(database, concepts, collectionName), IProductRepository
{
    public async Task<bool> TryTakeStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        long changed = await UpdateWhereAsync(
            product => product.Id == productId && product.IsActive && product.Stock >= quantity,
            set => set.Inc(product => product.Stock, -quantity),
            cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public async Task ReturnStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        await UpdateWhereAsync(product => product.Id == productId, set => set.Inc(product => product.Stock, quantity), cancellationToken).ConfigureAwait(false);
    }
}
