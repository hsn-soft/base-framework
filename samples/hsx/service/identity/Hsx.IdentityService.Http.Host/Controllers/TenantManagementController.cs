using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>What the system tenant does to have resellers.</summary>
[Route("api/identity-service/v1/commercial/tenant-management")]
public sealed class TenantManagementController(ITenantManagementAppService tenantManagement) : BaseServiceController
{
    /// <summary>A reseller with its administrator role and, when given, its first administrator. Only the system tenant.</summary>
    [HttpPost("resellers/create")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.TenantManagement.CreateReseller)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ProvisionedTenantDto> CreateResellerAsync([FromBody] CreateResellerDto input, CancellationToken cancellationToken = default)
        => tenantManagement.CreateResellerAsync(input, cancellationToken);
}
