using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;

public interface INormalizedAnalysisRepository : IReadOnlyGenericRepository<NormalizedAnalysis, Guid>
{
    Task<NormalizedAnalysis> CreateAsync(
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        NormalizedAnalysisStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null
    );

    Task<NormalizedAnalysis> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        NormalizedAnalysisStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null
    );

    Task<NormalizedAnalysis> SetOutlineResultAsync(Guid id, bool isOutlineSuccess, [CanBeNull] string errorMessage, [CanBeNull] List<KeyValuePair<Guid,string>> outlineResultList);

    Task<NormalizedAnalysis> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);

    Task<NormalizedAnalysis> FindByUniqueKeysAsync(Guid clientId, Guid analysisContentId, CancellationToken cancellationToken = default);
}