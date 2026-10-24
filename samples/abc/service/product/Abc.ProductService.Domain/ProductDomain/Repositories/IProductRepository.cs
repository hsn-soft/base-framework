using Abc.ProductService.Domain.ProductDomain.Entities;

namespace Abc.ProductService.Domain.ProductDomain.Repositories;

/// <summary>Where the products are kept. The domain says what it needs; the persistence project says how.</summary>
public interface IProductRepository
{
    Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The products that match <paramref name="filter"/>, by name, from <paramref name="skip"/> on, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<Product>> GetListAsync(ProductFilter filter, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>How many products match <paramref name="filter"/>.</summary>
    Task<long> CountAsync(ProductFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The products whose name or sku has <paramref name="searchText"/> in it (all of them when it is empty), by name, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<Product>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default);

    /// <summary>Whether a product has this sku (compared without regard to case); <paramref name="exceptId"/> leaves one product out.</summary>
    Task<bool> SkuExistsAsync(string sku, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}
