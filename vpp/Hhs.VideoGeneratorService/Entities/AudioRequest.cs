using Hhs.Shared.Events;
using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.VideoGeneratorService.Entities;

public sealed class AudioRequest
{
    [BsonId]
    public Guid Id { get; set; }

    // Audit Fields
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Correlation & Context
    public Guid? CorrelationId { get; set; }
    public Guid VideoRequestId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }

    // Subscription & Scope
    public string ScopeKey { get; set; } = default!;

    // Status & Configuration
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;
    public int SortOrder { get; set; }

    // Input Data
    public string InputText { get; set; } = default!;

    // Provider Configuration
    public string AudioProviderKey { get; set; } = default!;

    // Audio Generation (external provider)
    public string? AudioProviderTrackingId { get; set; }
    public string? AudioProviderUrl { get; set; }
    public string? AudioLocalPath { get; set; }
    public string? AudioStorageUrl { get; set; }
    public string? AudioCdnUrl { get; set; }
    public string? AudioCdnProviderKey { get; set; }

    // Polling & Retry
    public DateTime? NextProviderPollAtUtc { get; set; }
    public int ProviderPollingCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }
}
