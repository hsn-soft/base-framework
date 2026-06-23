using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerContentRepository : IReadOnlyGenericRepository<CustomerContent, Guid>
{
    Task<CustomerContent> CreateAsync(
        Guid customerId, ProductTypes productType,
        [NotNull] string slugKey,
        CustomerContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task<CustomerContent> CreateAsync(
        Guid id,
        Guid customerId, ProductTypes productType,
        [NotNull] string slugKey,
        CustomerContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task SetCustomerContentNormalizedReferenceAsync(Guid id, Guid normalizedRequestId);
    Task<CustomerContent> SetCustomerContentNormalizedResultAsync(Guid id, bool isNormalizedSuccess, Guid normalizedRequestId, DateTime? releaseTime);

    Task SetVideoGenerationApprovedAsync(Guid id);
    Task SetVideoGenerationRejectedAsync(Guid id, [CanBeNull] string rejectReason);

    Task SetCustomerContentVideoReferenceAsync(Guid id, Guid videoRequestId);
    Task<CustomerContent> SetCustomerContentVideoResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, [CanBeNull] string storageVideoUrl);
    Task<CustomerContent> SetCustomerContentStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);


    Task<List<Guid>> GetCustomerDailyTrendContentIdsAsync( [NotNull] string scopeKey, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetCustomerDailyAnalysisContentIdsAsync([NotNull] string scopeKey, CancellationToken cancellationToken = default);
}