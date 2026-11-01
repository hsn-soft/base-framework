using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Services;
using Hsx.ContentService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Http.Host.Controllers;

/// <summary>The analysis contents: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/analysis-contents")]
public sealed class AnalysisContentsController(IAnalysisContentAppService analysisContentAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<AnalysisContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => analysisContentAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<AnalysisContentDto>> GetPagedListAsync([FromBody] GetAnalysisContentsPaged pagedInput, CancellationToken cancellationToken = default)
        => analysisContentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] AnalysisContentCreateDto input, CancellationToken cancellationToken = default)
    {
        AnalysisContentDto created = await analysisContentAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] AnalysisContentUpdateDto input, CancellationToken cancellationToken = default)
    {
        await analysisContentAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await analysisContentAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
