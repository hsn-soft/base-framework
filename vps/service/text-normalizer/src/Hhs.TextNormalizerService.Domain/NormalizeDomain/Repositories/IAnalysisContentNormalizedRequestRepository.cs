using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;

public interface IAnalysisContentNormalizedRequestRepository : IMongoGenericRepository<AnalysisContentNormalizedRequest, Guid>
{
    Task<AnalysisContentNormalizedRequest?> GetByScopeKeyAndContentIdAsync(string scopeKey, Guid analysisContentId, CancellationToken cancellationToken = default);
    Task<List<AnalysisContentNormalizedRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
    Task<List<AnalysisContentNormalizedRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
