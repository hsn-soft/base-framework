using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;

/// <summary>Gives a role exactly these grants: what it had and is not here is taken away. A code given twice is one grant (the last value wins).</summary>
public sealed class SetRolePermissionsDto
{
    [Required]
    [MaxLength(1000)]
    public List<GrantInputDto> Grants { get; set; } = [];
}
