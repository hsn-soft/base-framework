using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/app-users")]
public sealed class AppUsersController(IAppUserAppService appUsers) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Read)]
    public Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => appUsers.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Read)]
    public Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync([FromBody] GetAppUsersPagedDto pagedInput, CancellationToken cancellationToken = default)
        => appUsers.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Create)]
    public Task<AppUserDto> CreateAsync([FromBody] AppUserCreateDto input, CancellationToken cancellationToken = default)
        => appUsers.CreateAsync(input, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Update)]
    public Task<AppUserDto> UpdateAsync([FromBody] AppUserUpdateDto input, CancellationToken cancellationToken = default)
        => appUsers.UpdateAsync(input, cancellationToken);

    [HttpPost("roles/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Read)]
    public Task<AppUserRolesDto> GetRolesAsync(Guid id, CancellationToken cancellationToken = default)
        => appUsers.GetRolesAsync(id, cancellationToken);

    [HttpPut("roles")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.SetRoles)]
    public Task<AppUserRolesDto> SetRolesAsync([FromBody] AppUserSetRolesDto input, CancellationToken cancellationToken = default)
        => appUsers.SetRolesAsync(input, cancellationToken);

    [HttpPut("temporary-password")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.SetPassword)]
    public Task<TemporaryPasswordIssuedDto> IssueTemporaryPasswordAsync([FromBody] IssueTemporaryPasswordDto input, CancellationToken cancellationToken = default)
        => appUsers.IssueTemporaryPasswordAsync(input, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AppUsers.Delete)]
    public Task<AppUserDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => appUsers.DeleteAsync(id, cancellationToken);
}
