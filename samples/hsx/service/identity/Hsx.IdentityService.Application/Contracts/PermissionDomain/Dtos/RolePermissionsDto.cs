using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

/// <summary>What a role was granted: the permissions and the constraints with their values.</summary>
public sealed class RolePermissionsDto : IBaseDto
{
    public Guid RoleId { get; init; }

    public List<GrantedPermissionDto> Grants { get; init; } = [];
}
