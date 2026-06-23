using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface IAnalysisContentAppService : IEventApplicationService
{
    Task SetAnalysisContentNormalizedReferenceAsync(Guid analysisContentId, Guid normalizedRequestId);
    Task SetAnalysisContentNormalizedResultAsync(Guid analysisContentId, Guid normalizedRequestId, bool isNormalizedSuccess, [CanBeNull] string correlationId = null);

    Task SetAnalysisContentVideoReferenceAsync(Guid analysisContentId, Guid videoRequestId);
    Task SetAnalysisContentVideoResultAsync(Guid analysisContentId, Guid videoRequestId, bool isGenerateSuccess,
        [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null);

    Task SetAnalysisContentStatusToFailedAsync(Guid analysisContentId, string failedReason, [CanBeNull] string correlationId = null);

    Task AnalysisVideoGenerationQueryAsync(AnalysisVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);

    Task<ForceAnalysisVideoGenerationResultDto> ForceAnalysisVideoGenerationQueryAsync(ForceAnalysisVideoGenerationRequestDto input, string correlationId = null);
}