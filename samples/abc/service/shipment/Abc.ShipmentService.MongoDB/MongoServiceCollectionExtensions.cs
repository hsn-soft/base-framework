using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using Abc.ShipmentService.MongoDB.Configurations;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ShipmentService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the shipment service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-ShipmentService</c>, and its readiness check), the mappings and the repositories.
    /// </summary>
    public static IServiceCollection AddShipmentPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoShipmentDbProperties.ConnectionStringName);

        ShipmentClassMap.Register();
        services.AddMongoRepository<Shipment, Guid>(MongoShipmentDbProperties.CollectionPrefix + ShipmentConsts.CollectionName);
        return services;
    }
}
