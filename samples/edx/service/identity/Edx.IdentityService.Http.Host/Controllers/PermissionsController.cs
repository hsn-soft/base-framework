using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The catalogue of the permissions of the system: what can be granted. It is the code's list, so it is read only.</summary>
[Route("api/identity-service/v1/commercial/permissions")]
public sealed class PermissionsController(IPermissionAppService permissions) : BaseServiceController
{
    [HttpPost("filter-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PermissionDto>> GetFilterListAsync([FromBody] GetPermissionsFilter filterInput, CancellationToken cancellationToken = default)
        => permissions.GetFilterListAsync(filterInput, cancellationToken);
}
