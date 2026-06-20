using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.TextNormalizerService.Entities;

public sealed class CustomerContentNormalizedRequest
{
    // Identity & Correlation
    [BsonId]
    public Guid Id { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }

    // Content Reference
    public Guid CustomerContentId { get; set; }
    public string Url { get; set; } = default!;

    // Status & Progress
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Scraping State
    public string? ScrapingStatus { get; set; }
    public ScrapingResult? ScrapingResult { get; set; }

    // Outline Generation State
    public string? OutlineStatus { get; set; }
    public OutlineResult? OutlineResult { get; set; }

    // Outline Polling & Tracking
    public string? OutlineProviderTrackId { get; set; }
    public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = 60;

    // Retry Configuration
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    // Audit Fields
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Provider Configuration
    public string OutlineProviderKey { get; set; } = default!;
    public string VideoProviderKey { get; set; } = default!;
    public string? AudioProviderKey { get; set; }
}
