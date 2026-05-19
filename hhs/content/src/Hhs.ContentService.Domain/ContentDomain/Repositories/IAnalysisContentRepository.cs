using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAnalysisContentRepository : IReadOnlyGenericRepository<AnalysisContent, Guid>
{
    Task<List<AnalysisContent>> GetPagedListWithFiltersAsync(
        Guid? clientId = null,
        DateTime? analysisStartDate = null,
        DateTime? analysisEndDate = null,
        AnalysisContentOperationStates? status = null,

        string sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default
    );

    Task<long> GetCountWithFiltersAsync(
        Guid? clientId = null,
        DateTime? analysisStartDate = null,
        DateTime? analysisEndDate = null,
        AnalysisContentOperationStates? status = null,
        CancellationToken cancellationToken = default
    );

    Task<List<AnalysisContent>> GetFilterListAsync(
        Guid? clientId = null,
        DateTime? analysisStartDate = null,
        DateTime? analysisEndDate = null,
        AnalysisContentOperationStates? status = null,
        string sorting = null,
        CancellationToken cancellationToken = default
    );

    Task<AnalysisContent> CreateAsync(
        Guid tenantId,
        Guid clientId,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        Guid? normalizedAnalysisId = null,
        Guid? videoRequestId = null,
        [CanBeNull] string storageVideoUrl = null,
        [CanBeNull] string correlationId = null);

    Task<AnalysisContent> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        Guid? normalizedAnalysisId = null,
        Guid? videoRequestId = null,
        [CanBeNull] string storageVideoUrl = null,
        [CanBeNull] string correlationId = null);

    Task SetNormalizedAnalysisReferenceAsync(Guid id, Guid normalizedAnalysisId);

    Task<AnalysisContent> SetNormalizedContentResultAsync(Guid id, bool isNormalizedSuccess, Guid normalizedAnalysisId);

    Task SetVideoRequestReferenceAsync(Guid id, Guid videoRequestId);

    Task<AnalysisContent> SetVideoGenerationResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, [CanBeNull] string storageVideoUrl);

    Task<AnalysisContent> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);
}