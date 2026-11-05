using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/permissions")]
public sealed class PermissionsController(IPermissionAppService permissions) : BaseServiceController
{
    [HttpPost("filter-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Permissions.Read)]
    public Task<ListDataResultDto<PermissionDto>> GetFilterListAsync([FromBody] GetPermissionsFilterDto filterInput, CancellationToken cancellationToken = default)
        => permissions.GetFilterListAsync(filterInput, cancellationToken);
}
