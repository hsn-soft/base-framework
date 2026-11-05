using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/tenant-management")]
public sealed class TenantManagementController(ITenantManagementAppService tenantManagement) : BaseServiceController
{
    [HttpPost("resellers/create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.TenantManagement.CreateReseller)]
    public Task<ProvisionedTenantDto> CreateResellerAsync([FromBody] CreateResellerDto input, CancellationToken cancellationToken = default)
        => tenantManagement.CreateResellerAsync(input, cancellationToken);
}
