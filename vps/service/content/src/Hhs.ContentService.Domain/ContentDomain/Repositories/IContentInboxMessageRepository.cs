using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IContentInboxMessageRepository : IGenericRepository<ContentInboxMessage, Guid>
{
    Task<ContentInboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<List<ContentInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<ContentInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
