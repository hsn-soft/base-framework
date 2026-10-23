using HsnSoft.Base.Domain.Entities.Events;

namespace Xyz.Shared.Events;

// The order flow:
//   Order    --OrderCreated-->      Product
//   Product  --StockReserved-->     Shipment, Order   (or --StockRejected--> Order)
//   Shipment --ShipmentDispatched-> Order

/// <summary>An order was placed and waits for a stock check.</summary>
public sealed record OrderCreated(Guid OrderId, Guid ProductId, int Quantity) : IIntegrationEventMessage;

/// <summary>The requested quantity was taken out of the stock.</summary>
public sealed record StockReserved(Guid OrderId, Guid ProductId, int Quantity, decimal UnitPrice) : IIntegrationEventMessage;

/// <summary>The stock could not be reserved (unknown product or not enough items).</summary>
public sealed record StockRejected(Guid OrderId, string Reason) : IIntegrationEventMessage;

/// <summary>The goods of an order left the warehouse.</summary>
public sealed record ShipmentDispatched(Guid OrderId, Guid ShipmentId, string TrackingNumber) : IIntegrationEventMessage;
