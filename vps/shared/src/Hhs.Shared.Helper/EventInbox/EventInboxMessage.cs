using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.Shared.Helper.EventInbox;

public sealed class EventInboxMessage : AuditedEntity<Guid>
{
    [CanBeNull]
    public string CorrelationId { get; private set; }
    [NotNull] public string EventName { get; private set; } = default!;
    [NotNull] public string Payload { get; private set; } = default!;
    [NotNull] public string Status { get; set; } = InboxStatuses.Started;
    public DateTime? ProcessedAtUtc { get; set; }
    [CanBeNull] public string ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    private EventInboxMessage() { }

    public EventInboxMessage(Guid id, string eventName, string payload, [CanBeNull] string correlationId = null)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        CorrelationId = correlationId;
    }
}
