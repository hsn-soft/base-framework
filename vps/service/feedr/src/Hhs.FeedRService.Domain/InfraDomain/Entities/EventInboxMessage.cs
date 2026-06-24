using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.FeedRService.Domain.InfraDomain.Entities;

public sealed class EventInboxMessage : AuditedEntity<Guid>
{
    public Guid? CorrelationId { get; private set; }
    public string EventName { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string Status { get; set; } = InboxStatuses.Started;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    private EventInboxMessage() { }

    public EventInboxMessage(Guid id, string eventName, string payload, Guid? correlationId = null)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        CorrelationId = correlationId;
    }
}
