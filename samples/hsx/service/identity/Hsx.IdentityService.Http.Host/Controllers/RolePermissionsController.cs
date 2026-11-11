using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/app-roles/{roleId:guid}/permissions")]
public sealed class RolePermissionsController(IRolePermissionAppService rolePermissions) : BaseServiceController
{
    [HttpPost("get")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.RolePermissions.Read)]
    public Task<RolePermissionsDto> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
        => rolePermissions.GetAsync(roleId, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.RolePermissions.Update)]
    public Task<RolePermissionsDto> SetAsync(Guid roleId, [FromBody] SetRolePermissionsDto input, CancellationToken cancellationToken = default)
        => rolePermissions.SetAsync(roleId, input, cancellationToken);
}
