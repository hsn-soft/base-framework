using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface IAnalysisContentAppService : IEventApplicationService
{
    Task SetNormalizedAnalysisReferenceAsync(Guid analysisContentId, Guid normalizedAnalysisId);

    Task SetNormalizedResultAsync(Guid analysisContentId, Guid normalizedAnalysisId, bool isNormalizedSuccess, [CanBeNull] string correlationId = null);

    Task SetVideoGenerationRequestReferenceAsync(Guid analysisContentId, Guid videoRequestId);

    Task SetVideoGenerationResultAsync(Guid analysisContentId, Guid videoRequestId, bool isGenerateSuccess,
        [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null);

    Task SetStatusToFailedAsync(Guid analysisContentId, string failedReason, [CanBeNull] string correlationId = null);

    Task AnalysisVideoGenerationQueryAsync(AnalysisVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);
}