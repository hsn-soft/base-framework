using Hhs.Shared.Helper;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoVideoGeneratorInboxMessageRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<VideoGeneratorInboxMessage, Guid>(provider, dbContext), IVideoGeneratorInboxMessageRepository
{
    public async Task<List<VideoGeneratorInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoGeneratorInboxMessage> { Filter = x => x.Status != InboxStatuses.Completed };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<VideoGeneratorInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoGeneratorInboxMessage> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
