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

    /// <summary>
    /// Atomically transitions the row identified by <paramref name="id"/> to 'Started' (bumping
    /// RetryCount, clearing ErrorMessage) if — and only if — its current state is either 'Failed' with
    /// RetryCount &lt; <paramref name="maxRetryCount"/>, or 'Started' with LastModificationTime older
    /// than <paramref name="staleStartedBeforeUtc"/> (the previous processing attempt is presumed
    /// abandoned — e.g. the process crashed and this call is happening because the broker redelivered
    /// the message). Implemented as a single atomic round trip (no read-then-write) so concurrent
    /// callers can never both succeed. Returns the number of rows affected (0 or 1).
    /// </summary>
    Task<int> TryReclaimAsync(Guid id, DateTime staleStartedBeforeUtc, int maxRetryCount, CancellationToken cancellationToken = default);
}
