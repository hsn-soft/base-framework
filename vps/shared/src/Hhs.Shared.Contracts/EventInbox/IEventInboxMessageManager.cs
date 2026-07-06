using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.EventInbox;

public interface IEventInboxMessageManager
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken);

    Task<bool> StartAsync<TEvent>(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken) where TEvent : IIntegrationEventMessage;

    Task CompleteAsync(Guid eventId, CancellationToken cancellationToken);

    Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken);

    /// <summary>
    /// Resets records stuck in 'Started' status older than <paramref name="staleThreshold"/> back to
    /// 'Failed', so the next broker re-delivery can attempt processing. Returns the number reset.
    /// </summary>
    Task<int> ResetStaleStartedMessagesAsync(DateTime staleThreshold, CancellationToken cancellationToken);
}
