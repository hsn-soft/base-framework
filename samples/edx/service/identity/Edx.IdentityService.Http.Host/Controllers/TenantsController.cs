using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The tenants: a customer's user sees its own tenant only, a user of the system tenant sees all.</summary>
[Route("api/identity-service/v1/commercial/tenants")]
public sealed class TenantsController(ITenantAppService tenants) : BaseServiceController
{
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Tenants.Read)]
    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<TenantDto>> GetFilterListAsync([FromBody] GetTenantsFilter filterInput, CancellationToken cancellationToken = default)
        => tenants.GetFilterListAsync(filterInput, cancellationToken);
}
