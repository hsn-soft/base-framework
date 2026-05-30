using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;

public interface INormalizedRequestAppService : IEventApplicationService
{
    Task<NormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<NormalizedRequestDto>> GetPagedListAsync(GetNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<NormalizedRequestDto>> GetFilterListAsync(GetNormalizedRequestsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<NormalizedRequestSearchDto>> GetSearchListAsync(GetNormalizedRequestsSearch searchInput, CancellationToken cancellationToken = default);

    Task CreateAsync(AppContentNormalizedStartedEto input, [CanBeNull] string correlationId = null);

    Task ScrapingAsync(Guid normalizedRequestId);

    Task OutlineAsync(Guid normalizedRequestId);

    Task VideoGenerationApprovedAsync(VideoGenerationApprovedEto input);

    Task SetStatusToFailedAsync(Guid normalizedRequestId, string failedReason, [CanBeNull] string correlationId = null);
}