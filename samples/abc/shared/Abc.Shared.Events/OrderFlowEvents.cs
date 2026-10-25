using HsnSoft.Base.Domain.Entities.Events;

namespace Abc.Shared.Events;

// The order flow. Every message is something that happened (an event): it is published to every service that listens, and the publisher does not know who they are. Who started the chain (the user, the tenant) is not in the
// messages: the envelope carries it, and a handler runs as that user.
//
//   Order    --OrderCreatedEto-->         Product
//   Product  --OrderStockReservedEto-->   Order (confirmed), Shipment (sends it)
//   Product  --OrderStockRejectedEto-->   Order (cancelled)
//   Shipment --ShipmentCompletedEto-->    Order (delivered, or cancelled when it failed)

/// <summary>One line of an order, as the product service needs it.</summary>
public sealed record OrderLineEto(Guid ProductId, int Quantity);

/// <summary>An order was made.</summary>
public sealed record OrderCreatedEto(Guid OrderId, string CustomerUserName, string ShippingAddress, IReadOnlyList<OrderLineEto> Lines) : IIntegrationEventMessage;

/// <summary>The stock of every line of an order was set aside.</summary>
public sealed record OrderStockReservedEto(Guid OrderId, string ShippingAddress) : IIntegrationEventMessage;

/// <summary>The stock of an order could not be set aside: nothing was taken, and <paramref name="Reason"/> says why.</summary>
public sealed record OrderStockRejectedEto(Guid OrderId, string Reason) : IIntegrationEventMessage;

/// <summary>How the delivery of an order ended.</summary>
public sealed record ShipmentCompletedEto(Guid OrderId, Guid ShipmentId, string TrackingNumber, bool IsSuccessful, string? Reason) : IIntegrationEventMessage;
