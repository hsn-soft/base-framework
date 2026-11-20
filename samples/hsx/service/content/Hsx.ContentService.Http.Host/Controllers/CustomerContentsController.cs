using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Services;
using Hsx.ContentService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Controllers;

/// <summary>The customer contents: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/customer-contents")]
public sealed class CustomerContentsController(ICustomerContentAppService customerContentAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(ContentPermissions.Endpoints.CustomerContents.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerContentAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(ContentPermissions.Endpoints.CustomerContents.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync([FromBody] GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default)
        => customerContentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(ContentPermissions.Endpoints.CustomerContents.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerContentCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerContentDto created = await customerContentAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(ContentPermissions.Endpoints.CustomerContents.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CustomerContentUpdateDto input, CancellationToken cancellationToken = default)
    {
        await customerContentAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(ContentPermissions.Endpoints.CustomerContents.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerContentAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
