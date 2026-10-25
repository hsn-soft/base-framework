using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Xyz.OrderService.Domain.OrderDomain.Entities;
using Xyz.OrderService.Infrastructure.Context;

namespace Xyz.OrderService.Infrastructure;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the order service needs of PostgreSQL: the <c>OrderDbContext</c> on the provider and the connection string that <see cref="EfCoreOrderDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-OrderService</c>), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddOrderPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<OrderDbContext>(configuration, providers => providers.UsePostgres(), EfCoreOrderDbProperties.Provider, EfCoreOrderDbProperties.ConnectionStringName);

        services.AddEfCoreRepository<OrderDbContext, Order, Guid>();
        return services;
    }
}
