using System.ComponentModel.DataAnnotations;
using Abc.ShipmentService.Domain.ShipmentDomain.Consts;

namespace Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;

public sealed class ShipmentUpdateDto
{
    [Required]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(ShipmentConsts.CarrierMaxLength)]
    public string Carrier { get; set; } = string.Empty;

    [Required]
    [StringLength(ShipmentConsts.AddressMaxLength)]
    public string Address { get; set; } = string.Empty;

    /// <summary>Preparing, InTransit, Delivered or Returned; a delivered or returned shipment cannot change its status.</summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;
}
