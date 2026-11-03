using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;

/// <summary>Gives a role exactly these permissions (by their codes): what it had and is not here is taken away.</summary>
public sealed class SetRolePermissionsDto
{
    [Required]
    [MaxLength(1000)]
    public List<string> Codes { get; set; } = [];
}
