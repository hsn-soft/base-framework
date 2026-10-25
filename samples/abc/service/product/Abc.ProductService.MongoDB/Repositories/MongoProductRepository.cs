using System.Text.RegularExpressions;
using Abc.ProductService.Domain.ProductDomain.Consts;
using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Domain.ProductDomain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB.Repositories;

public sealed class MongoProductRepository(IMongoDatabase database, MongoEntityConcepts concepts) : IProductRepository
{
    private IMongoCollection<Product> Products => database.GetCollection<Product>(ProductConsts.CollectionName);

    public async Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => await Products.Find(concepts.Visible(Builders<Product>.Filter.Eq(product => product.Id, id))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetListAsync(ProductFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        => await Products.Find(concepts.Visible(Filter(filter))).SortBy(product => product.NormalizedName).Skip(skip).Limit(take).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<long> CountAsync(ProductFilter filter, CancellationToken cancellationToken = default)
        => Products.CountDocumentsAsync(concepts.Visible(Filter(filter)), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default)
    {
        FilterDefinitionBuilder<Product> builder = Builders<Product>.Filter;
        FilterDefinition<Product> filter = builder.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            BsonRegularExpression part = Part(searchText);
            filter = builder.Or(builder.Regex(product => product.NormalizedName, part), builder.Regex(product => product.Sku, part));
        }

        return await Products.Find(concepts.Visible(filter)).SortBy(product => product.NormalizedName).Limit(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        // a sku is unique in the whole system: a deleted product's sku and another tenant's are taken too, so no filter of the concepts
        string normalized = Product.Normalize(sku);
        return Products.Find(product => product.Sku == normalized && (exceptId == null || product.Id != exceptId)).AnyAsync(cancellationToken);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        concepts.Created(product);
        return Products.InsertOneAsync(product, cancellationToken: cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        concepts.Modified(product);
        return Replace(product, cancellationToken);
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        // a product is marked as deleted, not removed
        return concepts.SoftDeleted(product) ? Replace(product, cancellationToken) : Products.DeleteOneAsync(stored => stored.Id == product.Id, cancellationToken);
    }

    private async Task Replace(Product product, CancellationToken cancellationToken)
        => await Products.ReplaceOneAsync(stored => stored.Id == product.Id, product, cancellationToken: cancellationToken).ConfigureAwait(false);

    private static BsonRegularExpression Part(string text) => new(Regex.Escape(Product.Normalize(text)));

    private static FilterDefinition<Product> Filter(ProductFilter filter)
    {
        FilterDefinitionBuilder<Product> builder = Builders<Product>.Filter;
        List<FilterDefinition<Product>> parts = [];
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            parts.Add(builder.Regex(product => product.NormalizedName, Part(filter.Name)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Sku))
        {
            parts.Add(builder.Regex(product => product.Sku, Part(filter.Sku)));
        }

        if (filter.IsActive is { } isActive)
        {
            parts.Add(builder.Eq(product => product.IsActive, isActive));
        }

        if (filter.InStock is { } inStock)
        {
            parts.Add(inStock ? builder.Gt(product => product.Stock, 0) : builder.Lte(product => product.Stock, 0));
        }

        return parts.Count == 0 ? builder.Empty : builder.And(parts);
    }
}
