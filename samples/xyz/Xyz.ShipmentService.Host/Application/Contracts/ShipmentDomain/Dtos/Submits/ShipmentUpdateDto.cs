using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.ShipmentDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;

public sealed class ShipmentUpdateDto
{
    [Required]
    [NotDefault]
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
