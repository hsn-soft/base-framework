using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

/// <summary>Gives a user exactly these roles (roles of its own tenant).</summary>
public sealed class AppUserSetRolesDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    public List<Guid>? RoleIds { get; set; }
}
