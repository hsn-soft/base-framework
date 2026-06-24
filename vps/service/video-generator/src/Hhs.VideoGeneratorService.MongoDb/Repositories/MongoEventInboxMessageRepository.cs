using Hhs.Shared.Helper;
using Hhs.VideoGeneratorService.Domain.InfraDomain.Entities;
using Hhs.VideoGeneratorService.Domain.InfraDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoEventInboxMessageRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<EventInboxMessage, Guid>(provider, dbContext), IEventInboxMessageRepository
{
    public async Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage> { Filter = x => x.Status != InboxStatuses.Completed };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
