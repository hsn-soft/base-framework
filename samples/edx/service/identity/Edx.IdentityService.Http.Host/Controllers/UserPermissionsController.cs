using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>What a user is granted of its own, besides what its roles give.</summary>
[Route("api/identity-service/v1/commercial/users/{userId:guid}/permissions")]
public sealed class UserPermissionsController(IUserPermissionAppService userPermissions) : BaseServiceController
{
    [HttpPost("get")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.UserPermissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<GrantedPermissionDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => userPermissions.GetAsync(userId, cancellationToken);

    /// <summary>Replaces what the user is granted of its own with exactly the permissions given.</summary>
    [HttpPut]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.UserPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetAsync(Guid userId, [FromBody] SetGrantsDto input, CancellationToken cancellationToken = default)
    {
        await userPermissions.SetAsync(userId, input, cancellationToken);
        return NoContent();
    }
}
