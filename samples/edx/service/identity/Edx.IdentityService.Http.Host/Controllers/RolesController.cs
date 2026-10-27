using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
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
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Roles.FilterList)]
    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<RoleDto>> GetFilterListAsync([FromBody] GetRolesFilter filterInput, CancellationToken cancellationToken = default)
        => roles.GetFilterListAsync(filterInput, cancellationToken);
}
