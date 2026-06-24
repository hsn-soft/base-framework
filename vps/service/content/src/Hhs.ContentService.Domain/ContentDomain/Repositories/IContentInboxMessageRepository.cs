using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Query;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IContentInboxMessageRepository : IGenericRepository<ContentInboxMessage, Guid>
{
    Task<ContentInboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<List<ContentInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<ContentInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    Task<int> UpdateByExpressionAsync(
        Expression<Func<ContentInboxMessage, bool>> predicate,
        Action<UpdateSettersBuilder<ContentInboxMessage>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
