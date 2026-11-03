using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/app-roles")]
public sealed class AppRolesController(IAppRoleAppService appRoleAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoleAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync([FromBody] GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default)
        => appRoleAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] AppRoleCreateDto input, CancellationToken cancellationToken = default)
    {
        AppRoleDto created = await appRoleAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] AppRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpPost("subscriptions/{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<Guid>> GetSubscriptionIdsAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoleAppService.GetSubscriptionIdsAsync(id, cancellationToken);

    [HttpPut("subscriptions")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.SetSubscriptions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSubscriptionsAsync([FromBody] AppRoleSetSubscriptionsDto input, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.SetSubscriptionsAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.AppRoles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
