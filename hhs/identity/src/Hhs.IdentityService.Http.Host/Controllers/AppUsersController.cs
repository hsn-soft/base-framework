using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Controllers.Base;
using Hhs.Shared.Contracts.Cache.ServicePermissions;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<List<AppUserDto>> GetSearchListAsync([FromBody] GetAppUsersSearch searchInput, CancellationToken cancellationToken = default)
        => await _appUserAppService.GetSearchListAsync(searchInput, cancellationToken);

    [Authorize(IdentityServicePermissions.AppUsers.Create)]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppUserDto> CreateAsync([FromBody] AppUserCreateDto input) => await _appUserAppService.CreateAsync(input);

    [Authorize(IdentityServicePermissions.AppUsers.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task UpdateAsync([FromBody] AppUserUpdateDto input) => await _appUserAppService.UpdateAsync(input);

    [Authorize(IdentityServicePermissions.AppUsers.Delete)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DeleteAsync(Guid id) => await _appUserAppService.DeleteAsync(id);
}