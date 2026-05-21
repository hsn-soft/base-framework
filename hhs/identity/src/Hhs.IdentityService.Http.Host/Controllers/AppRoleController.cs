using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Controllers.Base;
using Hhs.Shared.Contracts.Cache.ServicePermissions;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/app-roles")]
public sealed class AppRoleController : BaseServiceController
{
    private readonly IAppRoleAppService _appRoleAppService;

    public AppRoleController(IServiceProvider provider, IAppRoleAppService appRoleAppService) : base(provider)
    {
        _appRoleAppService = appRoleAppService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppRoleDto> GetAsync(Guid id) => await _appRoleAppService.GetAsync(id);

    [Authorize(IdentityServicePermissions.AppRoles.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync([FromBody] GetAppRolesPaged pagedInput) => await _appRoleAppService.GetPagedListAsync(pagedInput);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppRoleDto>> GetFilterListAsync([FromBody] GetAppRolesFilter filterInput) => await _appRoleAppService.GetFilterListAsync(filterInput);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppRoleDto>> GetSearchListAsync([FromBody] GetAppRolesSearch searchInput) => await _appRoleAppService.GetSearchListAsync(searchInput);

    [Authorize(IdentityServicePermissions.AppRoles.Create)]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppRoleDto> CreateAsync([FromBody] AppRoleCreateDto input) => await _appRoleAppService.CreateAsync(input);

    [Authorize(IdentityServicePermissions.AppRoles.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task UpdateAsync([FromBody] AppRoleUpdateDto input) => await _appRoleAppService.UpdateAsync(input);

    [Authorize(IdentityServicePermissions.AppRoles.Delete)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DeleteAsync(Guid id) => await _appRoleAppService.DeleteAsync(id);
}