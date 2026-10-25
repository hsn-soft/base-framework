using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ShipmentService.MongoDB;

public static class ShipmentDatabase
{
    /// <summary>Makes the indexes of the shipments (the tracking number is unique; the order and the tenant are looked up). MongoDB creates the database and the collection when the first index or shipment is written.</summary>
    public static Task EnsureShipmentDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Shipment collection", async cancellationToken =>
        {
            IMongoCollection<Shipment> shipments = services.GetRequiredService<IMongoDatabase>().GetCollection<Shipment>(MongoShipmentDbProperties.CollectionPrefix + ShipmentConsts.CollectionName);
            await shipments.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(shipment => shipment.TrackingNumber), new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(shipment => shipment.OrderId)),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(shipment => shipment.TenantId)),
                ],
                cancellationToken).ConfigureAwait(false);
        });
    }
}
