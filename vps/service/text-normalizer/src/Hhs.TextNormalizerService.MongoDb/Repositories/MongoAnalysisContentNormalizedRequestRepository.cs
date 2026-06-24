using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoAnalysisContentNormalizedRequestRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
) : MongoGenericRepository<AnalysisContentNormalizedRequest, Guid>(provider, dbContext), IAnalysisContentNormalizedRequestRepository
{
    public async Task<AnalysisContentNormalizedRequest?> GetByScopeKeyAndContentIdAsync(string scopeKey, Guid analysisContentId, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey && x.AnalysisContentId == analysisContentId,
            cancellationToken: cancellationToken
        );

    public async Task<List<AnalysisContentNormalizedRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<AnalysisContentNormalizedRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
