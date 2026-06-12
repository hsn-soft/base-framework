using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAnalysisContentRepository : IReadOnlyGenericRepository<AnalysisContent, Guid>
{
    Task<AnalysisContent> CreateAsync(
        Guid customerId, ProductTypes productType,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task<AnalysisContent> CreateAsync(
        Guid id,
        Guid customerId, ProductTypes productType,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task SetAnalysisContentNormalizedReferenceAsync(Guid id, Guid normalizedRequestId);
    Task<AnalysisContent> SetAnalysisContentNormalizedResultAsync(Guid id, bool isNormalizedSuccess, Guid normalizedRequestId);

    Task SetAnalysisContentVideoReferenceAsync(Guid id, Guid videoRequestId);
    Task<AnalysisContent> SetAnalysisContentVideoResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, [CanBeNull] string storageVideoUrl);

    Task<AnalysisContent> SetAnalysisContentStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);
}