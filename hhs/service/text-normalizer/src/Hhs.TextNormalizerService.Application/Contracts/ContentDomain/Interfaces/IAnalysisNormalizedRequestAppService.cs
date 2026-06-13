using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;

public interface IAnalysisNormalizedRequestAppService : IEventApplicationService
{
    Task<AnalysisNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<AnalysisNormalizedRequestDto>> GetPagedListAsync(GetAnalysisNormalizedRequestPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<AnalysisNormalizedRequestDto>> GetFilterListAsync(GetAnalysisNormalizedRequestFilter filterInput, CancellationToken cancellationToken = default);

    Task CreateAsync(AnalysisContentNormalizedStartedEto input, [CanBeNull] string correlationId = null);

    Task OutlineAsync(Guid analysisNormalizedRequestId);

    Task VideoGenerationApprovedAsync([NotNull]string scopeKey, Guid analysisContentId);

    Task SetStatusToFailedAsync(Guid analysisNormalizedRequestId, string failedReason, [CanBeNull] string correlationId = null);
}