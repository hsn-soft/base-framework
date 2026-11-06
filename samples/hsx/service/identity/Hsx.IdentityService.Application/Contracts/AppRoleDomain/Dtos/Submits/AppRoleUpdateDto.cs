using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

public sealed class AppRoleUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(AppRoleConsts.NameMaxLength)]
    public string? Name { get; set; }
}
