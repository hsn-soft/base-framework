using Abc.ShipmentService.Domain.CustomerDomain.Consts;
using Abc.ShipmentService.Domain.CustomerDomain.Entities;
using Abc.ShipmentService.Domain.PaymentDomain.Consts;
using Abc.ShipmentService.Domain.PaymentDomain.Entities;
using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ShipmentService.MongoDB;

public static class ShipmentDatabase
{
    /// <summary>Makes the indexes of the shipments (the tracking number is unique; the order and the tenant are looked up), of the customers (the user name is unique in a tenant) and of the payments (looked up by order and customer). MongoDB creates the database and the collection when the first index or shipment is written.</summary>
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

            IMongoCollection<Customer> customers = services.GetRequiredService<IMongoDatabase>().GetCollection<Customer>(MongoShipmentDbProperties.CollectionPrefix + CustomerConsts.CollectionName);
            await customers.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Customer>(Builders<Customer>.IndexKeys.Ascending(customer => customer.TenantId).Ascending(customer => customer.NormalizedUserName), new CreateIndexOptions { Unique = true }),
                ],
                cancellationToken).ConfigureAwait(false);

            IMongoCollection<Payment> payments = services.GetRequiredService<IMongoDatabase>().GetCollection<Payment>(MongoShipmentDbProperties.CollectionPrefix + PaymentConsts.CollectionName);
            await payments.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(payment => payment.OrderId)),
                    new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(payment => payment.CustomerId)),
                    new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(payment => payment.TenantId)),
                ],
                cancellationToken).ConfigureAwait(false);
        });
    }
}
