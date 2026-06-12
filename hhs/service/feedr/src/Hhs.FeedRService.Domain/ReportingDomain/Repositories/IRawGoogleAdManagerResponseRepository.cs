using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Enums;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.Domain.ReportingDomain.Repositories;

public interface IRawGoogleAdManagerResponseRepository : IReadOnlyGenericRepository<RawGoogleAdManagerResponse, Guid>
{
    Task<RawGoogleAdManagerResponse> InsertAsync(RawGoogleAdManagerResponse entity, CancellationToken cancellationToken = default);

    Task<List<RawGoogleAdManagerResponse>> GetUnprocessedAsync(int maxCount = 100, CancellationToken cancellationToken = default);

    Task<RawGoogleAdManagerResponse> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);

    Task UpdateProcessedStatusAsync(Guid id, DerivationStatus status, string error = null, CancellationToken cancellationToken = default);
}
