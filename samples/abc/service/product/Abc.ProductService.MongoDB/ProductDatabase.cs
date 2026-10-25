using Abc.ProductService.Domain.ProductDomain.Consts;
using Abc.ProductService.Domain.ProductDomain.Entities;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB;

public static class ProductDatabase
{
    /// <summary>
    /// Makes the indexes (the sku is unique; the tenant is looked up) and, when there is no product yet, adds two to try the service with, in the tenant <paramref name="tenantId"/>: one with 10 in stock
    /// and one out of stock (0). MongoDB creates the database and the collection when the first one is written.
    /// </summary>
    public static Task EnsureProductDatabaseAsync(this IServiceProvider services, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Product collection", async cancellationToken =>
        {
            IMongoCollection<Product> products = services.GetRequiredService<IMongoDatabase>().GetCollection<Product>(ProductConsts.CollectionName);
            await products.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(product => product.Sku), new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(product => product.TenantId)),
                ],
                cancellationToken).ConfigureAwait(false);

            if (await products.CountDocumentsAsync(FilterDefinition<Product>.Empty, cancellationToken: cancellationToken).ConfigureAwait(false) > 0)
            {
                return;
            }

            // the seed is made as a tenant: the products belong to it
            MongoEntityConcepts concepts = services.GetRequiredService<MongoEntityConcepts>();
            using (services.GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                Product[] seed =
                [
                    Product.Create("Desk Lamp", "LAMP-001", "An adjustable LED desk lamp.", 29.90m, 10),
                    Product.Create("Office Chair", "CHAIR-001", "An ergonomic office chair; sold out.", 189.00m, 0),
                ];
                foreach (Product product in seed)
                {
                    concepts.Created(product);
                }

                await products.InsertManyAsync(seed, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        });
    }
}
