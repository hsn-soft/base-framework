using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoVideoRequestRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<VideoRequest, Guid>(provider, dbContext), IVideoRequestRepository
{
    [ItemCanBeNull]
    public async Task<VideoRequest> GetByScopeKeyAndRefContentAsync(string scopeKey, Guid refContentId, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey && x.RefContentId == refContentId,
            cancellationToken: cancellationToken
        );

    public async Task<List<VideoRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoRequest> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<VideoRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoRequest> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }
}
