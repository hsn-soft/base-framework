using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;

public interface IAnalysisNormalizedRequestRepository : IReadOnlyGenericRepository<AnalysisNormalizedRequest, Guid>
{
    Task<AnalysisNormalizedRequest> CreateAsync(
        [NotNull] string scopeKey,
        [NotNull] string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        AnalysisNormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null
    );

    Task<AnalysisNormalizedRequest> CreateAsync(
        Guid id,
        [NotNull] string scopeKey,
        [NotNull] string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        AnalysisNormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null
    );

    Task<AnalysisNormalizedRequest> SetOutlineResultAsync(Guid id, bool isOutlineSuccess, [CanBeNull] string errorMessage, [CanBeNull] List<KeyValuePair<Guid,string>> outlineResultList);

    Task<AnalysisNormalizedRequest> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);
}