using HsnSoft.Base.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.Domain.ProductDomain.Repositories;
using Xyz.ProductService.Infrastructure.Context;

namespace Xyz.ProductService.Infrastructure.Repositories;

/// <summary>The products in PostgreSQL: what <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> does for any entity, and the changes of the stock, each one statement (<c>UPDATE ... SET Stock = Stock - n WHERE Stock &gt;= n</c>).</summary>
public sealed class EfCoreProductRepository(ProductDbContext db) : EfCoreRepository<ProductDbContext, Product, Guid>(db), IProductRepository
{
    public async Task<bool> TryTakeStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        int changed = await UpdateWhereAsync(
            product => product.Id == productId && product.IsActive && product.Stock >= quantity,
            set => set.SetProperty(product => product.Stock, product => product.Stock - quantity),
            cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public async Task ReturnStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        await UpdateWhereAsync(
            product => product.Id == productId,
            set => set.SetProperty(product => product.Stock, product => product.Stock + quantity),
            cancellationToken).ConfigureAwait(false);
    }
}
