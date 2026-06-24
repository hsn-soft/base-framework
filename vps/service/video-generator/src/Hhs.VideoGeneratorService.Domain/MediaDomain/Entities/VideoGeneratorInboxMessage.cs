using Hhs.Shared.Helper;
using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

public sealed class VideoGeneratorInboxMessage
{
    [BsonId]
    public Guid EventId { get; set; }

    public Guid? CorrelationId { get; set; }

    public string EventName { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string Status { get; set; } = InboxStatuses.Started;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
