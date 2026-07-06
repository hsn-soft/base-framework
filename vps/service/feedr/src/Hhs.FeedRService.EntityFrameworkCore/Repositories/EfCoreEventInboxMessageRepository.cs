using Hhs.FeedRService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.EventInbox;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.EntityFrameworkCore.Repositories;

public sealed class EfCoreEventInboxMessageRepository(
    IServiceProvider provider,
    FeedRServiceDbContext dbContext
) : EfCoreGenericRepository<EventInboxMessage, Guid>(provider, dbContext), IEventInboxMessageRepository
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
