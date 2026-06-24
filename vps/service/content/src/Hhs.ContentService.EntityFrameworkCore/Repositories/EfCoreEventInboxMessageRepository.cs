using Hhs.ContentService.Domain.InfraDomain.Entities;
using Hhs.ContentService.Domain.InfraDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreEventInboxMessageRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<EventInboxMessage, Guid>(provider, dbContext), IEventInboxMessageRepository
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

    public async Task<int> UpdateByExpressionAsync(
        System.Linq.Expressions.Expression<Func<EventInboxMessage, bool>> predicate,
        Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<EventInboxMessage>> setPropertyCalls,
        CancellationToken cancellationToken = default)
        => await base.UpdateByExpressionAsync(predicate, setPropertyCalls, cancellationToken);
}
