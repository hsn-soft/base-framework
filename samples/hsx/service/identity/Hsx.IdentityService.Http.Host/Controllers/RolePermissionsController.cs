using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>What the roles are granted. The administrator of a tenant changes the grants of the roles of its own tenant; the system tenant's, any tenant's.</summary>
[Route("api/identity-service/v1/commercial/app-roles/{roleId:guid}/permissions")]
public sealed class RolePermissionsController(IRolePermissionAppService rolePermissions) : BaseServiceController
{
    [HttpPost("get")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.RolePermissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
        => rolePermissions.GetAsync(roleId, cancellationToken);

    /// <summary>Replaces what the role is granted with exactly the permissions given.</summary>
    [HttpPut]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.RolePermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetAsync(Guid roleId, [FromBody] SetRolePermissionsDto input, CancellationToken cancellationToken = default)
    {
        await rolePermissions.SetAsync(roleId, input, cancellationToken);
        return NoContent();
    }
}
