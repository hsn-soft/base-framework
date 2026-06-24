using System.Linq.Expressions;
using Hhs.FeedRService.Domain.InfraDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Query;

namespace Hhs.FeedRService.Domain.InfraDomain.Repositories;

public interface IEventInboxMessageRepository : IGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    Task<int> UpdateByExpressionAsync(
        Expression<Func<EventInboxMessage, bool>> predicate,
        Action<UpdateSettersBuilder<EventInboxMessage>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
