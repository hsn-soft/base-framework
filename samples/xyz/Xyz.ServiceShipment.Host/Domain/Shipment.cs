using MongoDB.Bson.Serialization.Attributes;

namespace Xyz.ServiceShipment.Host.Domain;

public sealed class Shipment
{
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>One shipment per order (unique index): a redelivered StockReserved message cannot ship twice.</summary>
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public string TrackingNumber { get; set; } = string.Empty;

    public DateTimeOffset DispatchedAt { get; set; } = DateTimeOffset.UtcNow;
}
