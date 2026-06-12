using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;

public interface INormalizedRequestRepository : IReadOnlyGenericRepository<NormalizedRequest, Guid>
{
    Task<NormalizedRequest> CreateAsync(
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        Guid appContentId,
        [NotNull] string domainPath,
        NormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null,
        [CanBeNull] string outlineContentData = null,
        [CanBeNull] string correlationId = null
    );

    Task<NormalizedRequest> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        Guid appContentId,
        [NotNull] string domainPath,
        NormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null,
        [CanBeNull] string outlineContentData = null,
        [CanBeNull] string correlationId = null
    );

    Task<NormalizedRequest> SetScrapingResultAsync(Guid id, bool isScrapingSuccess, [CanBeNull] string errorMessage, [CanBeNull] ScrapingContentDataModel scrapingContentData);

    Task<NormalizedRequest> SetOutlineResultAsync(Guid id, bool isNormalizedSuccess, [CanBeNull] string errorMessage, [CanBeNull] string outlineResult);

    Task<NormalizedRequest> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);

    Task<NormalizedRequest> FindByUniqueKeysAsync(Guid clientId, Guid appContentId, CancellationToken cancellationToken = default);
}