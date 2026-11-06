using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Abc.ShipmentService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ShipmentService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shipment history context on the provider and the connection string that <see cref="EfCoreShipmentHistoryDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-ShipmentHistoryService</c>), with its readiness check and the
    /// repository: the second database of the shipment service, beside the MongoDB of its shipments.
    /// </summary>
    public static IServiceCollection AddShipmentHistoryPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<ShipmentHistoryDbContext>(configuration, providers => providers.UsePostgres(), EfCoreShipmentHistoryDbProperties.Provider, EfCoreShipmentHistoryDbProperties.ConnectionStringName);
        services.AddEfCoreRepository<ShipmentHistoryDbContext, ShipmentHistory, Guid>();
        // the database and its table are made when the host starts, by the host that owns it (SeedStartupConnections)
        return services.AddSchemaStartupStep<ShipmentHistoryDbContext>(EfCoreShipmentHistoryDbProperties.ConnectionStringName);
    }
}
