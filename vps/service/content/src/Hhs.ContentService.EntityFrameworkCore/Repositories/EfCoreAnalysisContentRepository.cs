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
    public async Task SetNormalizedReferenceAsync(Guid id, Guid normalizedRequestId,string normalizeStatus, string normalizeCurrentStep)
        => await UpdateByExpressionAsync(x => x.Id == id && x.NormalizeRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeRequestId, normalizedRequestId)
                .SetProperty(a => a.NormalizeStatus, normalizeStatus)
                .SetProperty(a => a.LastFacility, normalizeCurrentStep)
                .SetProperty(a => a.LastError, (string?)null)
        );

    public async Task SetVideoGenerationApprovedAsync(Guid id) =>
        await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeStatus, NormalizeStatusNames.Completed)
                .SetProperty(a => a.LastFacility, EventNames.NormalizerResultPublished)
                .SetProperty(a => a.LastError, (string)null)
                .SetProperty(a => a.VideoStatus, MediaStatusNames.Approved)
        );

    public async Task SetVideoReferenceAsync(Guid id, Guid videoRequestId)
        => await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.VideoRequestId, videoRequestId)
                .SetProperty(a => a.VideoStatus, MediaStatusNames.Created)
                .SetProperty(a => a.LastFacility, EventNames.VideoRequestCreated)
                .SetProperty(a => a.LastError, (string)null)
        );

    public async Task SetAudioOperationStartedAsync(Guid id, string audioMode)
        => await UpdateByExpressionAsync(x => x.Id == id && x.VideoStatus == MediaStatusNames.Created,
            s => s
                .SetProperty(a => a.VideoStatus, MediaStatusNames.AudioStarted)
                .SetProperty(a => a.LastFacility, audioMode)
                .SetProperty(a => a.LastError, (string)null)
        );

    public async Task SetVideoProviderStartedAsync(Guid id)
        => await UpdateByExpressionAsync(x => x.Id == id,
            s => s
                .SetProperty(a => a.VideoStatus, MediaStatusNames.VideoProviderStarted)
                .SetProperty(a => a.LastFacility, EventNames.VideoProviderRequestStarted)
                .SetProperty(a => a.LastError, (string)null)
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
