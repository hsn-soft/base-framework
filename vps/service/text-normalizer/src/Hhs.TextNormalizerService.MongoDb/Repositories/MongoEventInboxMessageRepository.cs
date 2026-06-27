using Hhs.Shared.Helper;
using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;
using Hhs.TextNormalizerService.Domain.InfraDomain.Repositories;
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
    public async Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage>
        {
            Filter = x => x.Status != InboxStatuses.Completed && x.Status != InboxStatuses.Failed
        };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<long> ResetStaleStartedMessagesAsync(DateTime staleThreshold, CancellationToken cancellationToken = default)
    {
        return await UpdateByExpressionAsync(
            x => x.Status == InboxStatuses.Started && x.CreationTime < staleThreshold,
            u => u
                .Set(x => x.Status, InboxStatuses.Failed)
                .Set(x => x.ErrorMessage, "Reset by retry worker: handler did not complete within the stale threshold."),
            cancellationToken);
    }
}
