using Hhs.Shared.Helper;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;

public sealed class AnalysisNormalizedItem
{
    // Identity & Ordering
    public Guid CustomerContentId { get; set; }
    public int SortOrder { get; set; }

    // Content Reference
    [NotNull] public string ContentKey { get; set; } = default!;

    // Scraping State
    [NotNull] public string ScrapingStatus { get; set; } = StatusNames.Created;
    [CanBeNull]  public ScrapingContentDataModel ScrapingResult { get; set; }

    // Outline Generation State
    [NotNull] public string OutlineStatus { get; set; } = StatusNames.Created;
    [CanBeNull]  public OutlineResult OutlineResult { get; set; }

    // Outline Polling & Tracking
    [CanBeNull]  public string OutlineProviderTrackId { get; set; }
    [CanBeNull] public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }

    // Status & Progress
    [NotNull] public string Status { get; set; } = StatusNames.Created;
    [NotNull] public string CurrentStep { get; set; } = StatusNames.Created;

    // Retry Configuration
    public int RetryCount { get; set; }

    [CanBeNull] public DateTime? NextRetryAtUtc { get; set; }

    // Error Handling & Tracking
    [CanBeNull]  public string LastError { get; set; }
    [CanBeNull] public DateTime? UpdatedAtUtc { get; set; }
}