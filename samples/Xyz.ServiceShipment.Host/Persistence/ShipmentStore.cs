using MongoDB.Driver;
using Xyz.ServiceShipment.Host.Domain;

namespace Xyz.ServiceShipment.Host.Persistence;

public sealed class ShipmentStore(IMongoDatabase database)
{
    private readonly IMongoCollection<Shipment> _shipments = database.GetCollection<Shipment>("shipments");

    public async Task InitializeAsync(CancellationToken cancellationToken)
        => await _shipments.Indexes.CreateOneAsync(
            new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.OrderId), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

    public async Task<Shipment?> FindByOrderAsync(Guid orderId, CancellationToken cancellationToken)
        => await _shipments.Find(x => x.OrderId == orderId).FirstOrDefaultAsync(cancellationToken);

    public async Task<Shipment?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await _shipments.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    /// <summary>Creates the shipment of an order, or returns the one that already exists.</summary>
    public async Task<Shipment> DispatchAsync(Guid orderId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var shipment = new Shipment
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            TrackingNumber = "XYZ-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
        };

        try
        {
            await _shipments.InsertOneAsync(shipment, cancellationToken: cancellationToken);
            return shipment;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await _shipments.Find(x => x.OrderId == orderId).FirstAsync(cancellationToken);
        }
    }
}
