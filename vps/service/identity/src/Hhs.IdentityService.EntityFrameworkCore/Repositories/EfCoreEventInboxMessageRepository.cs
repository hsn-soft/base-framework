using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.EventInbox;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public sealed class EfCoreEventInboxMessageRepository(
    IServiceProvider provider,
    IdentityServiceDbContext dbContext
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

    public async Task<int> TryReclaimAsync(Guid id, DateTime staleStartedBeforeUtc, int maxRetryCount, CancellationToken cancellationToken = default)
    {
        return await GetDbSet()
            .Where(x => x.Id == id && (
                (x.Status == InboxStatuses.Failed && x.RetryCount < maxRetryCount) ||
                (x.Status == InboxStatuses.Started && x.LastModificationTime < staleStartedBeforeUtc)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, InboxStatuses.Started)
                .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                .SetProperty(x => x.ErrorMessage, (string)null)
                .SetProperty(x => x.LastModificationTime, DateTime.UtcNow), cancellationToken);
    }
}
