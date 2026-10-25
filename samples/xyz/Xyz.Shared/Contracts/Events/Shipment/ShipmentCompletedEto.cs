using HsnSoft.Base.Domain.Entities.Events;

namespace Xyz.Shared.Contracts.Events.Shipment;

/// <summary>How the fulfilment of an order ended: delivered, or not (the payment was declined, or the carrier could not reach the address); a parcel that was never sent has no shipment. Published by the shipment service; the order service delivers the order, or cancels it when the delivery failed.</summary>
public sealed record ShipmentCompletedEto(Guid OrderId, Guid? ShipmentId, string? TrackingNumber, bool IsSuccessful, string? Reason) : IIntegrationEventMessage;
