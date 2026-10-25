using System.ComponentModel.DataAnnotations;
using Abc.ShipmentService.Domain.ShipmentDomain.Consts;

namespace Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;

public sealed class ShipmentCreateDto
{
    [Required]
    public Guid? OrderId { get; set; }

    [Required]
    [StringLength(ShipmentConsts.CarrierMaxLength)]
    public string Carrier { get; set; } = string.Empty;

    [Required]
    [StringLength(ShipmentConsts.AddressMaxLength)]
    public string Address { get; set; } = string.Empty;
}
