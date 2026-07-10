namespace Hhs.TextNormalizerService.Domain.Constants;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents. Deliberately separate from Milestones (which names the actual integration events
/// published on the bus) — a facility describes what's happening right now, not which event fired.
/// Milestones stays in use for retry-mechanism bookkeeping (CurrentMilestone/FailedMilestone values).
/// </summary>
public static class Facilities
{
    public const string NormalizeRequestCreated = "NORMALIZE_REQUEST_CREATED";

    public const string ScrapingStarted = "SCRAPING_STARTED";
    public const string ScrapingCompleted = "SCRAPING_COMPLETED";
    public const string ScrapingFailed = "SCRAPING_FAILED";

    public const string OutlineStarted = "OUTLINE_STARTED";
    public const string OutlineSkipped = "OUTLINE_SKIPPED";
    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderRequestCompleted = "OUTLINE_PROVIDER_REQUEST_COMPLETED";

    public const string OutlineProviderPollingStarted = "OUTLINE_PROVIDER_POLLING_STARTED";
    public const string OutlineProviderPolling = "OUTLINE_PROVIDER_POLLING";

    public const string OutlineCompleted = "OUTLINE_COMPLETED";
    public const string OutlineFailed = "OUTLINE_FAILED";

    public const string NormalizerResultPublished = "NORMALIZER_RESULT_PUBLISHED";
    public const string VideoGenerationDataForwarded = "VIDEO_GENERATION_DATA_FORWARDED";
    public const string RetryScheduled = "RETRY_SCHEDULED";
    public const string RetryAttempted = "RETRY_ATTEMPTED";
    public const string RetryAttemptFailed = "RETRY_ATTEMPT_FAILED";
    public const string MilestoneFailed = "MILESTONE_FAILED";

    public const string RetryDueRequestsTriggered = "RETRY_DUE_REQUESTS";
    public const string PollDueOutlineRequestsTriggered = "POLL_DUE_OUTLINE_REQUESTS";
    public const string CheckReadyAnalysisContentsToOutlineTriggered = "CHECK_READY_ANALYSIS_CONTENTS_TO_OUTLINE";
    public const string CheckReadyAnalysisContentsToResultTriggered = "CHECK_READY_ANALYSIS_CONTENTS_TO_RESULT";
}
