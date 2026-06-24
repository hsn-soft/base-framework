using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.InfraDomain.Repositories;

public interface IEventInboxMessageRepository : IMongoGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
