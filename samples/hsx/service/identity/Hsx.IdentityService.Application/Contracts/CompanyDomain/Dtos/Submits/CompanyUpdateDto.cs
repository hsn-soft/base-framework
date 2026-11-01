using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;

/// <summary>What can be changed of a company.</summary>
public sealed class CompanyUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(CompanyConsts.TitleMaxLength)]
    public string? Title { get; set; }
}
