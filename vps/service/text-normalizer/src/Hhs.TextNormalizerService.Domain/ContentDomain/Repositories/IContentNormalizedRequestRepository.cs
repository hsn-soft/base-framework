using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;

public interface IContentNormalizedRequestRepository : IReadOnlyGenericRepository<ContentNormalizedRequest, Guid>
{
    Task<ContentNormalizedRequest> CreateAsync(
        [NotNull] string scopeKey,
        [NotNull] string domainName,
        Guid customerContentId,
        [NotNull] string domainPath,
        ContentNormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null,
        [CanBeNull] string outlineContentData = null,
        [CanBeNull] string correlationId = null
    );

    Task<ContentNormalizedRequest> CreateAsync(
        Guid id,
        [NotNull] string scopeKey,
        [NotNull] string domainName,
        Guid customerContentId,
        [NotNull] string domainPath,
        ContentNormalizedRequestStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null,
        [CanBeNull] string outlineContentData = null,
        [CanBeNull] string correlationId = null
    );

    Task<ContentNormalizedRequest> SetScrapingResultAsync(Guid id, bool isScrapingSuccess, [CanBeNull] string errorMessage, [CanBeNull] ScrapingContentDataModel scrapingContentData);

    Task<ContentNormalizedRequest> SetOutlineResultAsync(Guid id, bool isNormalizedSuccess, [CanBeNull] string errorMessage, [CanBeNull] string outlineResult);

    Task<ContentNormalizedRequest> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);
}