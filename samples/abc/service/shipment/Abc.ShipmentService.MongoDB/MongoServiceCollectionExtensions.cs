using Abc.ShipmentService.Domain.ShipmentDomain.Repositories;
using Abc.ShipmentService.MongoDB.Configurations;
using Abc.ShipmentService.MongoDB.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ShipmentService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mappings and the repositories of the shipment service. The <c>IMongoDatabase</c> itself is registered by the host
    /// (<c>AddBaseMongo</c>, connection string <c>ConnectionStrings:Mongo</c>).
    /// </summary>
    public static IServiceCollection AddShipmentPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        ShipmentClassMap.Register();
        services.AddScoped<IShipmentRepository, MongoShipmentRepository>();
        return services;
    }
}
