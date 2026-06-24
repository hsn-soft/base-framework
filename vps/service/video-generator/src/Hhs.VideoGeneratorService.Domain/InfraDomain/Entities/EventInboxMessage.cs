using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.InfraDomain.Entities;

public sealed class EventInboxMessage : AuditedEntity<Guid>
{
    // Correlation & Tracing
    [CanBeNull]
    public string CorrelationId { get; private set; }

    // Event Data
    [NotNull] public string EventName { get; private set; } = default!;
    [NotNull] public string Payload { get; private set; } = default!;

    // Status & Tracking
    [NotNull] public string Status { get; set; } = InboxStatuses.Started;
    public DateTime? ProcessedAtUtc { get; set; }

    // Error Handling
    [CanBeNull]  public string ErrorMessage { get; set; }

    // Retry Management
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
