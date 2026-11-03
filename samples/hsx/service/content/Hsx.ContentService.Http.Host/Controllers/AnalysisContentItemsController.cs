using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Services;
using Hsx.ContentService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Http.Host.Controllers;

/// <summary>The analysis content items: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/analysis-content-items")]
public sealed class AnalysisContentItemsController(IAnalysisContentItemAppService analysisContentItemAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Content.AnalysisContentItems.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<AnalysisContentItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => analysisContentItemAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.AnalysisContentItems.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<AnalysisContentItemDto>> GetPagedListAsync([FromBody] GetAnalysisContentItemsPaged pagedInput, CancellationToken cancellationToken = default)
        => analysisContentItemAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.AnalysisContentItems.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] AnalysisContentItemCreateDto input, CancellationToken cancellationToken = default)
    {
        AnalysisContentItemDto created = await analysisContentItemAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(HsxEndpointPermissions.Content.AnalysisContentItems.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] AnalysisContentItemUpdateDto input, CancellationToken cancellationToken = default)
    {
        await analysisContentItemAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.AnalysisContentItems.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await analysisContentItemAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
