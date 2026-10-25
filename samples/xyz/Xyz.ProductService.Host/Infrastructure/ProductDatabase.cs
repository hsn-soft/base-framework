using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.Infrastructure.Context;

namespace Xyz.ProductService.Infrastructure;

public static class ProductDatabase
{
    /// <summary>
    /// Creates the product database and its table when they are not there and, when there is no product yet, adds two to try the service with, in the tenant <paramref name="tenantId"/>: one with 10 in stock and one out of stock (0).
    /// </summary>
    public static async Task EnsureProductDatabaseAsync(this IServiceProvider services, Guid tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await services.EnsureDatabaseCreatedAsync<ProductDbContext>().ConfigureAwait(false);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            ProductDbContext db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            if (await db.Products.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            // the seed is made as a tenant: the products belong to it
            using (scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                db.Products.AddRange(
                    Product.Create("Desk Lamp", "LAMP-001", "An adjustable LED desk lamp.", 29.90m, 10),
                    Product.Create("Office Chair", "CHAIR-001", "An ergonomic office chair; sold out.", 189.00m, 0));
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Creates the stock history database and its table when they are not there.</summary>
    public static Task EnsureProductStockDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.EnsureDatabaseCreatedAsync<StockDbContext>();
    }
}
