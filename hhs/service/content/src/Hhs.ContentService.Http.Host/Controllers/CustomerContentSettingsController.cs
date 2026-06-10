using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/customer-content-settings")]
public sealed class CustomerContentSettingsController(IServiceProvider provider, ICustomerContentSettingAppService clientAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CustomerContentSettingDto> GetClientAsync(Guid id, CancellationToken cancellationToken = default)
        => await clientAppService.GetAsync(id, cancellationToken);

    // [Authorize(ContentServicePermissions.Clients.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<CustomerContentSettingDto>> GetPagedListAsync([FromBody] GetCustomerContentSettingsPaged pagedInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<CustomerContentSettingDto>> GetFilterListAsync([FromBody] GetCustomerContentSettingsFilter filterInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<CustomerContentSettingSearchDto>> GetSearchListAsync([FromBody] GetCustomerContentSettingsSearch searchInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetSearchListAsync(searchInput, cancellationToken);

    // [Authorize(ContentServicePermissions.Clients.Create)]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CustomerContentSettingDto> CreateAsync([FromBody] CustomerContentSettingCreateDto input)
        => await clientAppService.CreateAsync(input);

    // [Authorize(ContentServicePermissions.Clients.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task UpdateAsync([FromBody] CustomerContentSettingUpdateDto input)
        => await clientAppService.UpdateAsync(input);

    // [Authorize(ContentServicePermissions.Clients.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task DeleteAsync(Guid id)
        => await clientAppService.DeleteAsync(id);
}