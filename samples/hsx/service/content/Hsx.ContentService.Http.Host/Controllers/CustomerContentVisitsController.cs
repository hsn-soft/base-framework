using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Services;
using Hsx.ContentService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Http.Host.Controllers;

/// <summary>The customer content visits: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/customer-content-visits")]
public sealed class CustomerContentVisitsController(ICustomerContentVisitAppService customerContentVisitAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVisits.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerContentVisitDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerContentVisitAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVisits.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerContentVisitDto>> GetPagedListAsync([FromBody] GetCustomerContentVisitsPaged pagedInput, CancellationToken cancellationToken = default)
        => customerContentVisitAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVisits.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerContentVisitCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerContentVisitDto created = await customerContentVisitAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVisits.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerContentVisitAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
