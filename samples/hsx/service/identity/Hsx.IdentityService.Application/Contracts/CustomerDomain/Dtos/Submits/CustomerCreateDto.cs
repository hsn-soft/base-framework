using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.CustomerDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Submits;

public sealed class CustomerCreateDto
{
    [Required]
    [NotDefault]
    public Guid? CompanyId { get; set; }

    [Required]
    [StringLength(CustomerConsts.DomainMaxLength)]
    public string? Domain { get; set; }
}
