using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Entities;
using Xyz.ProductService.Infrastructure.Context;

namespace Xyz.ProductService.Infrastructure;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the product context on the provider and the connection string that <see cref="EfCoreProductDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-ProductService</c>), with its readiness check and the repository.
    /// </summary>
    public static IServiceCollection AddProductPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<ProductDbContext>(configuration, providers => providers.UsePostgres(), EfCoreProductDbProperties.Provider, EfCoreProductDbProperties.ConnectionStringName);
        return services.AddEfCoreRepository<ProductDbContext, Product, Guid>();
    }

    /// <summary>
    /// Registers the stock history context on the provider and the connection string that <see cref="EfCoreStockDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-StockService</c>): the second database of the product
    /// service, beside the one of the products.
    /// </summary>
    public static IServiceCollection AddProductStockPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<StockDbContext>(configuration, providers => providers.UsePostgres(), EfCoreStockDbProperties.Provider, EfCoreStockDbProperties.ConnectionStringName);
        return services.AddEfCoreRepository<StockDbContext, ProductStockHistory, Guid>();
    }
}
