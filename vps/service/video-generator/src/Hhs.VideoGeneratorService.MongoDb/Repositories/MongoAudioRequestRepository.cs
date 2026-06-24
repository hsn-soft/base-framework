using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoAudioRequestRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<AudioRequest, Guid>(provider, dbContext), IAudioRequestRepository
{
    public async Task<List<AudioRequest>> GetByVideoRequestIdAsync(Guid videoRequestId, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == videoRequestId };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<AudioRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AudioRequest> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<AudioRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<AudioRequest> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
