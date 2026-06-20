using Hhs.Shared.Events;
using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.VideoGeneratorService.Entities;

public sealed class VideoRequest
{
    [BsonId]
    public Guid Id { get; set; }

    // Audit Fields
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Correlation & Context
    public Guid? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }

    // Subscription & Scope
    public string ScopeKey { get; set; } = default!;

    // Status & Configuration
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Input Data
    public string MediaInputJson { get; set; } = default!;

    // Provider Configuration
    public string? AudioProviderKey { get; set; }
    public string VideoProviderKey { get; set; } = default!;

    // Video Generation (external provider)
    public string? VideoProviderTrackingId { get; set; }
    public string? VideoProviderUrl { get; set; }
    public string? VideoLocalPath { get; set; }

    // Storage & CDN
    public string? VideoCdnProviderKey { get; set; }
    public string? VideoStorageUrl { get; set; }
    public string? VideoCdnUrl { get; set; }


    // Polling & Retry
    public DateTime? NextProviderPollAtUtc { get; set; }
    public int ProviderPollingCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }
}
