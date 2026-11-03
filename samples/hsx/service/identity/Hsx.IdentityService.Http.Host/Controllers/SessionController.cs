using Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Services;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>What the caller may do and see: its menu and its permissions. Any signed-in user may ask (they are its own); they need no permission.</summary>
[Route("api/identity-service/v1/commercial/me")]
public sealed class SessionController(IMenuAppService menus, ISessionPermissionAppService permissions) : BaseServiceController
{
    /// <summary>The menu of the back office for the caller: only what it may see, each page with what it needs.</summary>
    [HttpPost("menus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<MenuDto>> GetMenusAsync(CancellationToken cancellationToken = default)
        => menus.GetMyMenusAsync(cancellationToken);

    /// <summary>What the caller was granted, through its roles.</summary>
    [HttpPost("permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<GrantedPermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        => permissions.GetMineAsync(cancellationToken);
}
