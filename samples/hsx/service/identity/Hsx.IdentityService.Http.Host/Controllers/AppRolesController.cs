using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/app-roles")]
public sealed class AppRolesController(IAppRoleAppService appRoles) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Read)]
    public Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoles.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Read)]
    public Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync([FromBody] GetAppRolesPagedDto pagedInput, CancellationToken cancellationToken = default)
        => appRoles.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Create)]
    public Task<AppRoleDto> CreateAsync([FromBody] AppRoleCreateDto input, CancellationToken cancellationToken = default)
        => appRoles.CreateAsync(input, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Update)]
    public Task<AppRoleDto> UpdateAsync([FromBody] AppRoleUpdateDto input, CancellationToken cancellationToken = default)
        => appRoles.UpdateAsync(input, cancellationToken);

    [HttpPost("subscriptions/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Read)]
    public Task<AppRoleSubscriptionsDto> GetSubscriptionsAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoles.GetSubscriptionsAsync(id, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppRoles.Delete)]
    public Task<AppRoleDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoles.DeleteAsync(id, cancellationToken);
}
