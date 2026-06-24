using Hhs.FeedRService.Domain.InfraDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.InfraDomain.Repositories.PostgreSQL;
using Hhs.FeedRService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.EntityFrameworkCore.Repositories.PostgreSQL;

public sealed class EfCoreEventInboxMessageRepository(
    IServiceProvider provider,
    FeedRServiceDbContext dbContext
) : EfCoreGenericRepository<EventInboxMessage, Guid>(provider, dbContext), IEventInboxMessageRepository
{
    public async Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<EventInboxMessage> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
