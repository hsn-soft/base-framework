using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>The catalogue of permissions: what can be granted. The code of the system is its only author, so it can only be read.</summary>
[Route("api/identity-service/v1/commercial/permissions")]
public sealed class PermissionsController(IPermissionAppService permissionAppService) : BaseServiceController
{
    [HttpPost("filter-list")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PermissionDto>> GetFilterListAsync([FromBody] GetPermissionsFilter filterInput, CancellationToken cancellationToken = default)
        => permissionAppService.GetFilterListAsync(filterInput, cancellationToken);
}
