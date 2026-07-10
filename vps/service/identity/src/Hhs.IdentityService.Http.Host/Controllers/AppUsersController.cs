using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Controllers.Base;
using Hhs.Shared.Helper.Permissions;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/app-users")]
public sealed class AppUsersController : BaseServiceController
{
    private readonly IAppUserAppService _appUserAppService;

    public AppUsersController(IServiceProvider provider, IAppUserAppService appUserAppService) : base(provider)
    {
        _appUserAppService = appUserAppService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _appUserAppService.GetAsync(id, cancellationToken);

    [PermissionAuthorize(IdentityServicePermissions.AppUsers.PagedList)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync([FromBody] GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
        => await _appUserAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppUserDto>> GetFilterListAsync([FromBody] GetAppUsersFilter filterInput, CancellationToken cancellationToken = default)
        => await _appUserAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppUserSearchDto>> GetSearchListAsync([FromBody] GetAppUsersSearch searchInput, CancellationToken cancellationToken = default)
        => await _appUserAppService.GetSearchListAsync(searchInput, cancellationToken);

    [PermissionAuthorize(IdentityServicePermissions.AppUsers.Create)]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppUserDto> CreateAsync([FromBody] AppUserCreateDto input)
        => await _appUserAppService.CreateAsync(input);

    [PermissionAuthorize(IdentityServicePermissions.AppUsers.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task UpdateAsync([FromBody] AppUserUpdateDto input)
        => await _appUserAppService.UpdateAsync(input);

    [PermissionAuthorize(IdentityServicePermissions.AppUsers.Delete)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task DeleteAsync(Guid id)
        => await _appUserAppService.DeleteAsync(id);
}