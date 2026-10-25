using Abc.ProductService.Domain.ProductDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Abc.ProductService.Domain.ProductDomain.Repositories;

/// <summary>
/// Where the products are kept: what every repository does (<see cref="IRepository{TEntity, TKey}"/>) and the two changes of the stock that must be one step in the database. A stock that is read, checked and written back by the code
/// is wrong as soon as two orders reach the service at once (both read 5, both take 3); here the check and the change are one statement of the store, so the stock never goes below zero and nothing taken is lost.
/// </summary>
public interface IProductRepository : IRepository<Product, Guid>
{
    /// <summary>Takes <paramref name="quantity"/> out of the stock of the product, only when it is active and has at least that much, and says whether it did. Whatever the number of callers at once, the stock never goes below zero.</summary>
    Task<bool> TryTakeStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Puts <paramref name="quantity"/> back into the stock of the product (what an attempt took and then had to give up).</summary>
    Task ReturnStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
