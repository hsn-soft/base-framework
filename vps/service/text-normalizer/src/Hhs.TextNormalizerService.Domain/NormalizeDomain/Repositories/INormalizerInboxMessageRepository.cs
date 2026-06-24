using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;

public interface INormalizerInboxMessageRepository : IGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
