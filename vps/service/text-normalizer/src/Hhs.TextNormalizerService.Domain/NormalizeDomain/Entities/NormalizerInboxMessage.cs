using Hhs.Shared.Helper;
using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;

public sealed class NormalizerInboxMessage
{
    // Identity
    [BsonId]
    public Guid EventId { get; set; }

    // Correlation & Tracing
    public string? CorrelationId { get; set; }

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
