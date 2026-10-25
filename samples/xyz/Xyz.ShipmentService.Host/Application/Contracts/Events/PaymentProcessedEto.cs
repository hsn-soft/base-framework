using HsnSoft.Base.Domain.Entities.Events;

namespace Xyz.ShipmentService.Application.Contracts.Events;

/// <summary>
/// The payment of an order was processed (it succeeded, or it was declined). Internal: published and handled by the shipment service itself, which sends the parcel when the payment succeeded and tells the order service
/// that the order will not be delivered when it did not.
/// </summary>
public sealed record PaymentProcessedEto(Guid OrderId, Guid PaymentId, bool IsSuccessful, string? Reason, string ShippingAddress) : IIntegrationEventMessage;
