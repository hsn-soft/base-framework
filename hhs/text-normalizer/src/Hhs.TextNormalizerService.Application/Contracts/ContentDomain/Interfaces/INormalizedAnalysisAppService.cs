using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;

public interface INormalizedAnalysisAppService : IEventApplicationService
{
    Task<NormalizedAnalysisDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<NormalizedAnalysisDto>> GetPagedListAsync(GetNormalizedAnalysisPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<NormalizedAnalysisDto>> GetFilterListAsync(GetNormalizedAnalysisFilter filterInput, CancellationToken cancellationToken = default);

    Task CreateAsync(AnalysisContentNormalizedStartedEto input, [CanBeNull] string correlationId = null);

    Task OutlineAsync(Guid normalizedAnalysisId);

    Task VideoGenerationApprovedAsync(VideoGenerationApprovedEto input);

    Task SetStatusToFailedAsync(Guid normalizedAnalysisId, string failedReason, [CanBeNull] string correlationId = null);
}