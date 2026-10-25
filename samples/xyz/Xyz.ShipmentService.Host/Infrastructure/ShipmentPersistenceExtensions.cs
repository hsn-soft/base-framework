using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Xyz.ShipmentService.Domain.CustomerDomain.Entities;
using Xyz.ShipmentService.Domain.PaymentDomain.Entities;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;
using Xyz.ShipmentService.Infrastructure.Context;

namespace Xyz.ShipmentService.Infrastructure;

public static class ShipmentPersistenceExtensions
{
    /// <summary>
    /// Registers the shipment context on the provider and the connection string that <see cref="EfCoreShipmentDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-ShipmentService</c>), with its readiness check and the repositories
    /// of the shipments, the customers and the payments.
    /// </summary>
    public static IServiceCollection AddShipmentPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<ShipmentDbContext>(configuration, providers => providers.UsePostgres(), EfCoreShipmentDbProperties.Provider, EfCoreShipmentDbProperties.ConnectionStringName);
        return services
            .AddEfCoreRepository<ShipmentDbContext, Shipment, Guid>()
            .AddEfCoreRepository<ShipmentDbContext, Customer, Guid>()
            .AddEfCoreRepository<ShipmentDbContext, Payment, Guid>();
    }

    /// <summary>Creates the shipment database and its tables when they are not there.</summary>
    public static Task EnsureShipmentDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.EnsureDatabaseCreatedAsync<ShipmentDbContext>();
    }
}
