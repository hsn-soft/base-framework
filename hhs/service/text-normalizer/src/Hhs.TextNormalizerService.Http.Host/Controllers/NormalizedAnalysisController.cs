using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Controllers;

[Route("api/text-normalizer-service/v1/commercial/normalized-analysis")]
public sealed class NormalizedAnalysisController(IServiceProvider provider, IAnalysisNormalizedRequestAppService normalizedAnalysisAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AnalysisNormalizedRequestDto> GetNormalizedAnalysisAsync(Guid id, CancellationToken cancellationToken = default)
        => await normalizedAnalysisAppService.GetAsync(id, cancellationToken);

    // [Authorize(TextNormalizerServicePermissions.NormalizedAnalysis.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<AnalysisNormalizedRequestDto>> GetPagedListAsync([FromBody] GetAnalysisNormalizedRequestPaged pagedInput, CancellationToken cancellationToken = default)
        => await normalizedAnalysisAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AnalysisNormalizedRequestDto>> GetFilterListAsync([FromBody] GetAnalysisNormalizedRequestFilter filterInput, CancellationToken cancellationToken = default)
        => await normalizedAnalysisAppService.GetFilterListAsync(filterInput, cancellationToken);
}