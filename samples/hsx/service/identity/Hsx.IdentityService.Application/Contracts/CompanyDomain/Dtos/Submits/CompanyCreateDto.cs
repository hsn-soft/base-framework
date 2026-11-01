using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;

public sealed class CompanyCreateDto
{
    [Required]
    [StringLength(CompanyConsts.TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    [StringLength(CompanyConsts.NameMaxLength)]
    public string? Name { get; set; }
}
