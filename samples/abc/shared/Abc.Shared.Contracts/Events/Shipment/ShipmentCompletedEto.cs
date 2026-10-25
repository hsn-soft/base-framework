using HsnSoft.Base.Domain.Entities.Events;

namespace Abc.Shared.Contracts.Events.Shipment;

/// <summary>How the delivery of an order ended. Published by the shipment service; the order service delivers the order, or cancels it when the delivery failed.</summary>
public sealed record ShipmentCompletedEto(Guid OrderId, Guid ShipmentId, string TrackingNumber, bool IsSuccessful, string? Reason) : IIntegrationEventMessage;
