using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAnalysisContentRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<AnalysisContent, Guid>(provider, dbContext), IAnalysisContentRepository
{
    public async Task SetNormalizedReferenceAsync(Guid id, Guid normalizedRequestId)
        => await UpdateByExpressionAsync(x => x.Id == id && x.NormalizeRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeRequestId, normalizedRequestId)
                .SetProperty(a => a.NormalizeStatus, StatusNames.Created)
                .SetProperty(a => a.LastFacility, EventNames.CustomerContentNormalizeRequestCreated)
        );

    public async Task SetVideoReferenceAsync(Guid id, Guid videoRequestId)
        => await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.VideoRequestId, videoRequestId)
                .SetProperty(a => a.VideoStatus, StatusNames.Created)
                .SetProperty(a => a.LastFacility, EventNames.VideoRequestCreated)
        );

    public async Task SetVideoGenerationApprovedAsync(Guid id) =>
        await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeStatus, StatusNames.Completed)
                .SetProperty(a => a.LastFacility, EventNames.NormalizerResultPublished)
                .SetProperty(a => a.LastError, (string)null)
                .SetProperty(a => a.VideoStatus, StatusNames.Approved)
        );

    [ItemCanBeNull]
    public async Task<AnalysisContent> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.Id == id,
            q => q.AsTracking().Include(x => x.Items),
            cancellationToken: cancellationToken
        );

    [ItemCanBeNull]
    public async Task<AnalysisContent> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey,
            cancellationToken: cancellationToken
        );

    public async Task<List<AnalysisContent>> GetAllByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AnalysisContent> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }
}
