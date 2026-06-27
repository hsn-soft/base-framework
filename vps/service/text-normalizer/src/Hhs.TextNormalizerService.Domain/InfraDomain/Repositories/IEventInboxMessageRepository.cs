using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.InfraDomain.Repositories;

public interface IEventInboxMessageRepository : IMongoGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all EventInboxMessage records stuck in 'Started' status older than
    /// <paramref name="staleThreshold"/> to 'Failed', so the next broker re-delivery
    /// can attempt processing instead of being silently skipped.
    /// </summary>
    Task<long> ResetStaleStartedMessagesAsync(DateTime staleThreshold, CancellationToken cancellationToken = default);
}
