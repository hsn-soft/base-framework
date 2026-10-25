using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.ShipmentDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;

public sealed class ShipmentCreateDto
{
    [Required]
    [NotDefault]
    public Guid? OrderId { get; set; }

    [Required]
    [StringLength(ShipmentConsts.CarrierMaxLength)]
    public string Carrier { get; set; } = string.Empty;

    [Required]
    [StringLength(ShipmentConsts.AddressMaxLength)]
    public string Address { get; set; } = string.Empty;
}
