using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>What the caller itself can do. Any signed-in user may ask: it is its own, and a client uses it to show what the user may use. It needs no permission of its own.</summary>
[Route("api/identity-service/v1/commercial/me/permissions")]
public sealed class SessionPermissionsController(ISessionPermissionAppService sessionPermissions) : BaseServiceController
{
    [HttpPost("get")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<GrantedPermissionDto>> GetAsync(CancellationToken cancellationToken = default)
        => sessionPermissions.GetMineAsync(cancellationToken);
}
