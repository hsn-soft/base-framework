using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

internal static class GrantMapping
{
    public static List<GrantedPermissionDto> ToDtos(IReadOnlyList<GrantedPermission> granted)
        => [.. granted.Select(item => new GrantedPermissionDto { Code = item.Code, Type = item.Type.ToString() })];
}
