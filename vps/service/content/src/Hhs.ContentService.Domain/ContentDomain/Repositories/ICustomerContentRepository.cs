using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerContentRepository : IGenericRepository<CustomerContent, Guid>
{
    Task SetNormalizedReferenceAsync(Guid id, Guid normalizedRequestId);
    Task SetScrapeTimeAsync(Guid id, DateTime? scrapeTime);

    Task SetVideoReferenceAsync(Guid id, Guid videoRequestId);
    Task SetVideoGenerationApprovedAsync(Guid id);
    Task SetVideoGenerationRejectedAsync(Guid id, [CanBeNull] string rejectReason);
    Task SetVideoGenerationSkippedAsync(Guid id, [CanBeNull] string skipReason);



    Task<CustomerContent> CreateAsync([NotNull] string scopeKey, [NotNull] string contentKey, [CanBeNull] string correlationId = null);


    [ItemCanBeNull]
    Task<CustomerContent> GetByScopeKeyAndSlugKeyAsync(string scopeKey, string slugKey, CancellationToken cancellationToken = default);
    [ItemCanBeNull]
    Task<CustomerContent> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CustomerContent>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);


    Task<List<Guid>> GetCustomerDailyTrendContentIdsAsync( [NotNull] string scopeKey, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetCustomerDailyAnalysisContentIdsAsync([NotNull] string scopeKey, CancellationToken cancellationToken = default);
}