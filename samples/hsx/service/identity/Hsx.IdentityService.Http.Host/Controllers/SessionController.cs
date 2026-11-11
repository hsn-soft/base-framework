using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Services;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

/// <summary>What the caller sees of itself: its menu and what it was granted. Any signed-in user (no permission asked: it is its own).</summary>
[Route("api/identity-service/v1/commercial/me")]
public sealed class SessionController(IMenuAppService menus, ISessionPermissionAppService permissions) : BaseServiceController
{
    [HttpPost("menus")]
    public Task<ListDataResultDto<MenuDto>> GetMenusAsync(CancellationToken cancellationToken = default)
        => menus.GetMyMenusAsync(cancellationToken);

    [HttpPost("permissions")]
    public Task<MyPermissionsDto> GetPermissionsAsync(CancellationToken cancellationToken = default)
        => permissions.GetMineAsync(cancellationToken);
}
