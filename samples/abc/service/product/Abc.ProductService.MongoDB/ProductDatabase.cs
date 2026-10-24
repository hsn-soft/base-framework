using Abc.ProductService.Domain.ProductDomain.Consts;
using Abc.ProductService.Domain.ProductDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB;

public static class ProductDatabase
{
    /// <summary>The user the seed writes as the author of the first products.</summary>
    public const string SeedAuthor = "system";

    /// <summary>
    /// Makes the unique index on the sku and, when there is no product yet, adds two to try the service with: one with 10 in stock and one out of stock (0). MongoDB creates the
    /// database and the collection when the first one is written.
    /// </summary>
    public static Task EnsureProductDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Product collection", async cancellationToken =>
        {
            IMongoCollection<Product> products = services.GetRequiredService<IMongoDatabase>().GetCollection<Product>(ProductConsts.CollectionName);
            await products.Indexes.CreateOneAsync(
                new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(product => product.Sku), new CreateIndexOptions { Unique = true }),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (await products.CountDocumentsAsync(FilterDefinition<Product>.Empty, cancellationToken: cancellationToken).ConfigureAwait(false) > 0)
            {
                return;
            }

            DateTimeOffset now = (services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();
            await products.InsertManyAsync(
                [
                    Product.Create("Desk Lamp", "LAMP-001", "An adjustable LED desk lamp.", 29.90m, 10, now, SeedAuthor),
                    Product.Create("Office Chair", "CHAIR-001", "An ergonomic office chair; sold out.", 189.00m, 0, now, SeedAuthor),
                ],
                cancellationToken: cancellationToken).ConfigureAwait(false);
        });
    }
}
