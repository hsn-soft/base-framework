using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Xyz.ShipmentService.Infrastructure.Context;

namespace Xyz.ShipmentService.Infrastructure;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shipment history context on the provider and the connection string that <see cref="EfCoreShipmentHistoryDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-ShipmentHistoryService</c>), with its readiness check and the
    /// repository: the second database of the shipment service, beside the one of its shipments.
    /// </summary>
    public static IServiceCollection AddShipmentHistoryPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<ShipmentHistoryDbContext>(configuration, providers => providers.UsePostgres(), EfCoreShipmentHistoryDbProperties.Provider, EfCoreShipmentHistoryDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();
        return services.AddEfCoreRepository<ShipmentHistoryDbContext, ShipmentHistory, Guid>();
    }

    /// <summary>Creates the shipment history database and its table when they are not there.</summary>
    public static Task EnsureShipmentHistoryDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.EnsureDatabaseCreatedAsync<ShipmentHistoryDbContext>();
    }
}
