using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

public sealed class VideoGeneratorInboxMessage : AuditedEntity<Guid>
{
    // Correlation & Tracing
    public Guid? CorrelationId { get; private set; }

    // Event Data
    public string EventName { get; private set; } = default!;
    public string Payload { get; private set; } = default!;

    // Status & Tracking
    public string Status { get; set; } = InboxStatuses.Started;
    public DateTime? ProcessedAtUtc { get; set; }

    // Error Handling
    public string? ErrorMessage { get; set; }

    // Retry Management
    public int RetryCount { get; set; } = 0;

    private VideoGeneratorInboxMessage() { }

    public VideoGeneratorInboxMessage(Guid id, string eventName, string payload, Guid? correlationId = null)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        CorrelationId = correlationId;
    }
}
