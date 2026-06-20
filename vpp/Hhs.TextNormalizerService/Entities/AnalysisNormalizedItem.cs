using Hhs.Shared.Events;

namespace Hhs.TextNormalizerService.Entities;

public sealed class AnalysisNormalizedItem
{
    // Identity & Ordering
    public Guid CustomerContentId { get; set; }
    public int SortOrder { get; set; }

    // Content Reference
    public string ContentKey { get; set; } = default!;

    // Scraping State
    public string ScrapingStatus { get; set; } = StatusNames.Created;
    public ScrapingResult? ScrapingResult { get; set; }

    // Outline Generation State
    public string OutlineStatus { get; set; } = StatusNames.Created;
    public OutlineResult? OutlineResult { get; set; }

    // Outline Polling & Tracking
    public string? OutlineProviderTrackId { get; set; }
    public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = 60;

    // Status & Progress
    public string Status { get; set; } = StatusNames.Created;
    public string CurrentStep { get; set; } = StatusNames.Created;

    // Retry Configuration
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }

    // Error Handling & Tracking
    public string? LastError { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
