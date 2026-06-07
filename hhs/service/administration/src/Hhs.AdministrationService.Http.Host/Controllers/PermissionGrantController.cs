using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AdministrationService.Controllers;

[Authorize(Roles = "system-admin")]
[Route("api/administration-service/v1/commercial/permission-grants")]
public sealed class PermissionGrantController(
    IServiceProvider provider,
    IPermissionGrantAppService permissionGrantAppService
) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PermissionGrantDto> GetAsync(Guid id) => permissionGrantAppService.GetAsync(id);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<PermissionGrantDto>> GetPagedListAsync([FromBody] GetPermissionGrantsPaged pagedInput) => permissionGrantAppService.GetPagedListAsync(pagedInput);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PermissionGrantDto>> GetFilterListAsync([FromBody] GetPermissionGrantsFilter filterInput) => permissionGrantAppService.GetFilterListAsync(filterInput);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PermissionGrantSearchDto>> GetSearchListAsync([FromBody] GetPermissionGrantsSearch searchInput) => permissionGrantAppService.GetSearchListAsync(searchInput);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PermissionGrantDto> CreateAsync([FromBody] PermissionGrantCreateDto input) => permissionGrantAppService.CreateAsync(input);

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task UpdateAsync([FromBody] PermissionGrantUpdateDto input) => permissionGrantAppService.UpdateAsync(input);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task DeleteAsync(Guid id) => permissionGrantAppService.DeleteAsync(id);
}