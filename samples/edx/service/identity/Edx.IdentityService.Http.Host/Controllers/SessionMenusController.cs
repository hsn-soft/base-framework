using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The menu of the back office for the caller: only what it may see, each page with what it needs. Any signed-in user may ask (the menu is its own); it needs no permission.</summary>
[Route("api/identity-service/v1/commercial/me/menus")]
public sealed class SessionMenusController(IMenuAppService menus) : BaseServiceController
{
    [HttpPost("get")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<MenuDto>> GetAsync(CancellationToken cancellationToken = default)
        => menus.GetMyMenusAsync(cancellationToken);
}
