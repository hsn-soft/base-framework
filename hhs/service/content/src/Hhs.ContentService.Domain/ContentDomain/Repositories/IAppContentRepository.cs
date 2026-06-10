using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAppContentRepository : IReadOnlyGenericRepository<AppContent, Guid>
{
    Task<AppContent> CreateAsync(
        Guid customerId,
        Guid productTypeId,
        [NotNull] string slugKey,
        AppContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task<AppContent> CreateAsync(
        Guid id,
        Guid customerId,
        Guid productTypeId,
        [NotNull] string slugKey,
        AppContentOperationStates operationStatus,
        [CanBeNull] string correlationId = null);

    Task SetNormalizedRequestReferenceAsync(Guid id, Guid normalizedRequestId);

    Task<AppContent> SetNormalizedContentResultAsync(Guid id,
        bool isNormalizedSuccess,
        Guid normalizedRequestId,
        DateTime? releaseTime);

    Task SetVideoGenerationApprovedAsync(Guid id);
    Task SetVideoGenerationRejectedAsync(Guid id, [CanBeNull] string rejectReason);

    Task SetVideoRequestReferenceAsync(Guid id, Guid videoRequestId);

    Task<AppContent> SetVideoGenerationResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, [CanBeNull] string storageVideoUrl);

    Task<AppContent> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);

    Task RemoveAsync(Guid id);

    Task<List<Guid>> GetCustomerDailyTrendContentIdsAsync(Guid customerId, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetCustomerDailyAnalysisContentIdsAsync(Guid customerId, CancellationToken cancellationToken = default);
}