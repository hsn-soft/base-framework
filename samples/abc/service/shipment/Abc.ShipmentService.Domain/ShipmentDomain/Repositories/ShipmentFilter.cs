using Abc.ShipmentService.Domain.ShipmentDomain.Entities;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Repositories;

/// <summary>What a list of shipments is narrowed by: every given value must match (the tracking number and the carrier by a part of them, without regard to case).</summary>
public sealed record ShipmentFilter(Guid? OrderId = null, string? TrackingNumber = null, string? Carrier = null, ShipmentStatus? Status = null);
