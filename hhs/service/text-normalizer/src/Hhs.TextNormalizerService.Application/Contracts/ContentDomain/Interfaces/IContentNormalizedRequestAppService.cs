using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;

public interface IContentNormalizedRequestAppService : IEventApplicationService
{
    Task<ContentNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<ContentNormalizedRequestDto>> GetPagedListAsync(GetContentNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<ContentNormalizedRequestDto>> GetFilterListAsync(GetContentNormalizedRequestsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<ContentNormalizedRequestSearchDto>> GetSearchListAsync(GetContentNormalizedRequestsSearch searchInput, CancellationToken cancellationToken = default);

    Task CreateAsync(CustomerContentNormalizedStartedEto input, [CanBeNull] string correlationId = null);

    Task ScrapingAsync(Guid contentNormalizedRequestId);

    Task OutlineAsync(Guid contentNormalizedRequestId);

    Task VideoGenerationApprovedAsync([NotNull]string scopeKey, Guid customerContentId);

    Task SetStatusToFailedAsync(Guid contentNormalizedRequestId, string failedReason, [CanBeNull] string correlationId = null);
}