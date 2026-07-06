using System.Linq.Expressions;

namespace Hhs.Shared.Helper.EventInbox;

public interface IEventInboxMessageRepository
{
    Task<bool> ExistsAsync(Expression<Func<EventInboxMessage, bool>> filter, CancellationToken cancellationToken = default);

    Task<EventInboxMessage> GetByIdOrDefaultAsync(Guid id, Func<IQueryable<EventInboxMessage>, IQueryable<EventInboxMessage>> includeEntity = null, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(EventInboxMessage entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(EventInboxMessage entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records stuck in 'Started' status older than <paramref name="staleThreshold"/> — a handler
    /// crashed after inserting the inbox record but before calling CompleteAsync/FailAsync.
    /// </summary>
    Task<List<EventInboxMessage>> GetStaleStartedMessagesAsync(DateTime staleThreshold, CancellationToken cancellationToken = default);
}
