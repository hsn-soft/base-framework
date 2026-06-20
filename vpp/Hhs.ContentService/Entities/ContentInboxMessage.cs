using Hhs.Shared.Inbox;

namespace Hhs.ContentService.Entities;

public sealed class ContentInboxMessage
{
    // Identity
    public Guid EventId { get; set; }

    // Correlation & Tracing
    public Guid? CorrelationId { get; set; }

    // Event Data
    public string EventName { get; set; } = default!;
    public string Payload { get; set; } = default!;

    // Status & Tracking
    public string Status { get; set; } = InboxStatuses.Started;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    // Error Handling
    public string? ErrorMessage { get; set; }
}
