using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

/// <summary>What the caller was granted, through its roles (the highest value when two roles give a constraint a number).</summary>
public sealed class MyPermissionsDto : IBaseDto
{
    public List<GrantedPermissionDto> Grants { get; init; } = [];
}
