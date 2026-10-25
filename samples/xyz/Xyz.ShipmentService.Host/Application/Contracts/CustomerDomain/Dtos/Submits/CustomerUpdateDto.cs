using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.CustomerDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Submits;

/// <summary>What can be changed of a customer (the user name is what finds it and stays).</summary>
public sealed class CustomerUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

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
