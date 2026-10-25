using HsnSoft.Base.Domain.Entities.Events;

namespace Abc.Shared.Events;

// The order flow (every message carries the tenant of the order, so the receiving service acts in it):
//
//   Order    --OrderCreatedEto-->        Product
//   Product  --OrderStockCheckedEto-->   Order      (the stock was enough, or it was not)
//   Product  --ShipmentRequestedEto-->   Shipment   (only when the stock was enough: ship it)
//   Shipment --ShipmentCompletedEto-->   Order      (delivered, or it failed)

/// <summary>One line of an order, as the product service needs it.</summary>
public sealed record OrderLineEto(Guid ProductId, int Quantity);

/// <summary>An order was made and waits for a stock check.</summary>
public sealed record OrderCreatedEto(Guid OrderId, Guid TenantId, string CustomerUserName, string ShippingAddress, IReadOnlyList<OrderLineEto> Lines) : IIntegrationEventMessage;

/// <summary>The answer of the stock check: the stock of every line was taken out for the order (<paramref name="IsAvailable"/>), or nothing was and <paramref name="Reason"/> says why.</summary>
public sealed record OrderStockCheckedEto(Guid OrderId, Guid TenantId, bool IsAvailable, string? Reason) : IIntegrationEventMessage;

/// <summary>The stock of an order is set aside: the parcel can be sent to the address.</summary>
public sealed record ShipmentRequestedEto(Guid OrderId, Guid TenantId, string ShippingAddress) : IIntegrationEventMessage;

/// <summary>How the delivery of an order ended.</summary>
public sealed record ShipmentCompletedEto(Guid OrderId, Guid TenantId, Guid ShipmentId, string TrackingNumber, bool IsSuccessful, string? Reason) : IIntegrationEventMessage;
