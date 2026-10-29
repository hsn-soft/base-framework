using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Filters;

public sealed class GetShipmentsPaged : PagedDataRequestDto
{
    /// <summary>Only the shipments of this order.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>A part of the tracking number.</summary>
    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    /// <summary>A part of the carrier's name.</summary>
    [StringLength(100)]
    public string? Carrier { get; set; }

    /// <summary>Only the shipments in this status (Preparing, InTransit, Delivered, Returned); all when it is not given.</summary>
    [StringLength(20)]
    public string? Status { get; set; }
}
