using Microsoft.Extensions.DependencyInjection;

namespace Abc.ShipmentService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the repositories of the shipment service (they are added here as the domain has them). The <c>IMongoDatabase</c> itself is registered by the host
    /// (<c>AddBaseMongo</c>, connection string <c>ConnectionStrings:Mongo</c>).
    /// </summary>
    public static IServiceCollection AddShipmentPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
