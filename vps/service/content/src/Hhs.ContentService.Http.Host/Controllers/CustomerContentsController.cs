using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/customer-contents")]
public sealed class CustomerContentsController(
    IServiceProvider provider,
    ICustomerContentAppService appContentAppService
) : BaseServiceController(provider)
{
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await appContentAppService.GetAsync(id, cancellationToken);

    // [Authorize(ContentServicePermissions.AppContents.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync([FromBody] GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default)
        => await appContentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<CustomerContentDto>> GetFilterListAsync([FromBody] GetCustomerContentsFilter filterInput, CancellationToken cancellationToken = default)
        => await appContentAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<CustomerContentSearchDto>> GetSearchListAsync([FromBody] GetCustomerContentsSearch searchInput, CancellationToken cancellationToken = default)
        => await appContentAppService.GetSearchListAsync(searchInput, cancellationToken);
}