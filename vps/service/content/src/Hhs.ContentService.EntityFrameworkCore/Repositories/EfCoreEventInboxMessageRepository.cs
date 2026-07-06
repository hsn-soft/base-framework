using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.EventInbox;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreEventInboxMessageRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
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
