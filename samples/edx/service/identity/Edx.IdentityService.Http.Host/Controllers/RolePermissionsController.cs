using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>What the roles are granted. A tenant's administrator changes the grants of its own roles; the system tenant's, any tenant's.</summary>
[Route("api/identity-service/v1/commercial/roles/{roleId:guid}/permissions")]
public sealed class RolePermissionsController(IRolePermissionAppService rolePermissions) : BaseServiceController
{
    [HttpPost("get")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.RolePermissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
        => rolePermissions.GetAsync(roleId, cancellationToken);

    /// <summary>Replaces what the role is granted with exactly the permissions given.</summary>
    [HttpPut]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.RolePermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetAsync(Guid roleId, [FromBody] SetGrantsDto input, CancellationToken cancellationToken = default)
    {
        await rolePermissions.SetAsync(roleId, input, cancellationToken);
        return NoContent();
    }
}
