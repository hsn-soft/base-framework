using System.ComponentModel.DataAnnotations;
using Xyz.ShipmentService.Domain.CustomerDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Submits;

public sealed class CustomerCreateDto
{
    [Required]
    [StringLength(CustomerConsts.UserNameMaxLength)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(CustomerConsts.FullNameMaxLength)]
    public string? FullName { get; set; }

    [EmailAddress]
    [StringLength(CustomerConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(CustomerConsts.PhoneMaxLength)]
    public string? Phone { get; set; }

    [StringLength(CustomerConsts.AddressMaxLength)]
    public string? Address { get; set; }
}
