using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/tenants")]
public sealed class TenantsController(ITenantAppService tenants) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Read)]
    public Task<TenantDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => tenants.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Read)]
    public Task<PagedDataResultDto<TenantDto>> GetPagedListAsync([FromBody] GetTenantsPagedDto pagedInput, CancellationToken cancellationToken = default)
        => tenants.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Create)]
    public Task<TenantDto> CreateAsync([FromBody] TenantCreateDto input, CancellationToken cancellationToken = default)
        => tenants.CreateAsync(input, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Update)]
    public Task<TenantDto> UpdateAsync([FromBody] TenantUpdateDto input, CancellationToken cancellationToken = default)
        => tenants.UpdateAsync(input, cancellationToken);

    [HttpPut("move")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Move)]
    public Task<TenantDto> MoveAsync([FromBody] TenantMoveDto input, CancellationToken cancellationToken = default)
        => tenants.MoveAsync(input, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Tenants.Delete)]
    public Task<TenantDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => tenants.DeleteAsync(id, cancellationToken);
}
