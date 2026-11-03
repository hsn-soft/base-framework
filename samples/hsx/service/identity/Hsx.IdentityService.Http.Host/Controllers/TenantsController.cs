using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/tenants")]
public sealed class TenantsController(ITenantAppService tenantAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<TenantDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => tenantAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<TenantDto>> GetPagedListAsync([FromBody] GetTenantsPaged pagedInput, CancellationToken cancellationToken = default)
        => tenantAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] TenantCreateDto input, CancellationToken cancellationToken = default)
    {
        TenantDto created = await tenantAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] TenantUpdateDto input, CancellationToken cancellationToken = default)
    {
        await tenantAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    /// <summary>Puts a tenant, with the tenants under it, under another (or at the root).</summary>
    [HttpPut("move")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Move)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MoveAsync([FromBody] TenantMoveDto input, CancellationToken cancellationToken = default)
    {
        await tenantAppService.MoveAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Tenants.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await tenantAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
