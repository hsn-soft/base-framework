using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Filters;

public sealed class GetShipmentHistoriesPaged : PagedDataRequestDto
{
    /// <summary>Only the history of this shipment.</summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>Only the history of the shipments of this order.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>A part of the tracking number.</summary>
    [StringLength(32)]
    public string? TrackingNumber { get; set; }

    /// <summary>How the delivery ended (<c>delivered</c>, <c>failed</c>, ...); the exact word.</summary>
    [StringLength(32)]
    public string? Outcome { get; set; }
}
