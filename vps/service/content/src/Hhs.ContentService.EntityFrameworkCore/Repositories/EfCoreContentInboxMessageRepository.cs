using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreContentInboxMessageRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<ContentInboxMessage, Guid>(provider, dbContext), IContentInboxMessageRepository
{
    public async Task<ContentInboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.Id == eventId,
            cancellationToken: cancellationToken
        );

    public async Task<List<ContentInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<ContentInboxMessage>
        {
            Filter = x => x.Status != InboxStatuses.Completed && x.Status != InboxStatuses.Failed
        };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<ContentInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<ContentInboxMessage> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<int> UpdateByExpressionAsync(
        System.Linq.Expressions.Expression<Func<ContentInboxMessage, bool>> predicate,
        Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<ContentInboxMessage>> setPropertyCalls,
        CancellationToken cancellationToken = default)
        => await base.UpdateByExpressionAsync(predicate, setPropertyCalls, cancellationToken);
}
