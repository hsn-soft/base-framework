namespace Xyz.Shared.Events;

/// <summary>Base of every message that travels through the broker.</summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

// The order flow:
//   Order    --OrderCreated-->      Product
//   Product  --StockReserved-->     Shipment, Order   (or --StockRejected--> Order)
//   Shipment --ShipmentDispatched-> Order

/// <summary>An order was placed and waits for a stock check.</summary>
public sealed record OrderCreated(Guid OrderId, Guid ProductId, int Quantity) : IntegrationEvent;

/// <summary>The requested quantity was taken out of the stock.</summary>
public sealed record StockReserved(Guid OrderId, Guid ProductId, int Quantity, decimal UnitPrice) : IntegrationEvent;

/// <summary>The stock could not be reserved (unknown product or not enough items).</summary>
public sealed record StockRejected(Guid OrderId, string Reason) : IntegrationEvent;

/// <summary>The goods of an order left the warehouse.</summary>
public sealed record ShipmentDispatched(Guid OrderId, Guid ShipmentId, string TrackingNumber) : IntegrationEvent;
