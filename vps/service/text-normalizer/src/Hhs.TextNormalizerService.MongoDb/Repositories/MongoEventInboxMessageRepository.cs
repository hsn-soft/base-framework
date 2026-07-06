using Hhs.Shared.Helper;
using Hhs.Shared.Helper.EventInbox;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoEventInboxMessageRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
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

    public async Task<int> TryReclaimAsync(Guid id, DateTime staleStartedBeforeUtc, int maxRetryCount, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EventInboxMessage>.Filter;
        var reclaimFilter = filter.And(
            filter.Eq(x => x.Id, id),
            filter.Or(
                filter.And(
                    filter.Eq(x => x.Status, InboxStatuses.Failed),
                    filter.Lt(x => x.RetryCount, maxRetryCount)),
                filter.And(
                    filter.Eq(x => x.Status, InboxStatuses.Started),
                    filter.Lt(x => x.LastModificationTime, staleStartedBeforeUtc))));

        var update = Builders<EventInboxMessage>.Update
            .Set(x => x.Status, InboxStatuses.Started)
            .Inc(x => x.RetryCount, 1)
            .Set(x => x.ErrorMessage, null)
            .Set(x => x.LastModificationTime, DateTime.UtcNow);

        var result = await GetCollection().UpdateOneAsync(reclaimFilter, update, cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }
}
