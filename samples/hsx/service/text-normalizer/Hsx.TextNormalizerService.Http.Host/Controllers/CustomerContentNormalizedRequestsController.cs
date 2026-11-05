using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.Shared.Contracts.Permissions;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Filters;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Submits;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Services;
using Hsx.TextNormalizerService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.TextNormalizerService.Http.Host.Controllers;

/// <summary>The customer content normalized requests: every action only calls the application service, which holds the rules.</summary>
[Route("api/text-normalizer-service/v1/commercial/customer-content-normalized-requests")]
public sealed class CustomerContentNormalizedRequestsController(ICustomerContentNormalizedRequestAppService customerContentNormalizedRequestAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerContentNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerContentNormalizedRequestAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerContentNormalizedRequestDto>> GetPagedListAsync([FromBody] GetCustomerContentNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default)
        => customerContentNormalizedRequestAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerContentNormalizedRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerContentNormalizedRequestDto created = await customerContentNormalizedRequestAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CustomerContentNormalizedRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        await customerContentNormalizedRequestAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerContentNormalizedRequestAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
