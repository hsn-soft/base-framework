using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Xyz.ServiceProduct.Host.Domain;

public sealed class Product
{
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    public int Stock { get; set; }
}

public enum ReservationStatus
{
    /// <summary>Claimed by the handler, the stock is being checked.</summary>
    Processing,
    Reserved,
    Rejected,
}

/// <summary>
/// One document per order. Its id is the order id, so the same OrderCreated message delivered twice cannot take the stock
/// twice: the second insert fails and the stored result is announced again.
/// </summary>
public sealed class StockReservation
{
    [BsonId]
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReservationStatus Status { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
