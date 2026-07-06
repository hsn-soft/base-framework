using Hhs.Shared.Helper;
using Hhs.Shared.Helper.EventInbox;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoEventInboxMessageRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<EventInboxMessage, Guid>(provider, dbContext), IEventInboxMessageRepository
{
    public async Task<List<EventInboxMessage>> GetStaleStartedMessagesAsync(DateTime staleThreshold, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage>
        {
            Filter = x => x.Status == InboxStatuses.Started && x.CreationTime < staleThreshold
        };
        return await GetListAsync(options, cancellationToken);
    }
}
