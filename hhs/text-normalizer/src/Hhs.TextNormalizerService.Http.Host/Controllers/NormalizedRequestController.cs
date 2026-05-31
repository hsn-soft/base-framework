using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Controllers;

[Route("api/text-normalizer-service/v1/commercial/normalized-requests")]
public sealed class NormalizedRequestController(IServiceProvider provider, INormalizedRequestAppService normalizedRequestAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<NormalizedRequestDto> GetNormalizedRequestAsync(Guid id, CancellationToken cancellationToken = default)
        => await normalizedRequestAppService.GetAsync(id, cancellationToken);

    [PermissionAuthorize("invoice.read")]
    // [Authorize(TextNormalizerServicePermissions.NormalizedRequests.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<NormalizedRequestDto>> GetPagedListAsync([FromBody] GetNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default)
        => await normalizedRequestAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<NormalizedRequestDto>> GetFilterListAsync([FromBody] GetNormalizedRequestsFilter filterInput, CancellationToken cancellationToken = default)
        => await normalizedRequestAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<NormalizedRequestSearchDto>> GetSearchListAsync([FromBody] GetNormalizedRequestsSearch searchInput, CancellationToken cancellationToken = default)
        => await normalizedRequestAppService.GetSearchListAsync(searchInput, cancellationToken);
}