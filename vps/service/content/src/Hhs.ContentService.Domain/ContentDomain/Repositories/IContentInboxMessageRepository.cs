using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Query;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IContentInboxMessageRepository : IGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    Task<int> UpdateByExpressionAsync(
        Expression<Func<EventInboxMessage, bool>> predicate,
        Action<UpdateSettersBuilder<EventInboxMessage>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
