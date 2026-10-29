using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The roles, by the same rule as the users.</summary>
[Route("api/identity-service/v1/commercial/roles")]
public sealed class RolesController(IRoleAppService roles) : BaseServiceController
{
    [HttpPost("filter-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Roles.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<RoleDto>> GetFilterListAsync([FromBody] GetRolesFilter filterInput, CancellationToken cancellationToken = default)
        => roles.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Roles.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] RoleCreateDto input, CancellationToken cancellationToken = default)
    {
        RoleDto created = await roles.CreateAsync(input, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Roles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await roles.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
