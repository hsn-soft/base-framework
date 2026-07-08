namespace Hhs.TextNormalizerService.Application.Consts;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents. Deliberately separate from EventNames (which names the actual integration events
/// published on the bus) — a facility describes what's happening right now, not which event fired.
/// EventNames stays in use for retry-mechanism bookkeeping (CurrentStep/FailedStep values).
/// </summary>
public static class Facilities
{
    public const string NormalizeRequestCreated = "NORMALIZE_REQUEST_CREATED";
    public const string ScrapingStarted = "SCRAPING_STARTED";
    public const string ScrapingCompleted = "SCRAPING_COMPLETED";
    public const string OutlineStarted = "OUTLINE_STARTED";
    public const string OutlineSkipped = "OUTLINE_SKIPPED";
    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderRequestCompleted = "OUTLINE_PROVIDER_REQUEST_COMPLETED";
    public const string OutlineProviderPollingStarted = "OUTLINE_PROVIDER_POLLING_STARTED";
    public const string OutlineCompleted = "OUTLINE_COMPLETED";
    public const string NormalizerResultPublished = "NORMALIZER_RESULT_PUBLISHED";
    public const string VideoGenerationDataForwarded = "VIDEO_GENERATION_DATA_FORWARDED";
    public const string RetryScheduled = "RETRY_SCHEDULED";
    public const string StepFailed = "STEP_FAILED";

    public const string RetryDueRequestsTriggered = "RETRY_DUE_REQUESTS";
    public const string PollDueOutlineRequestsTriggered = "POLL_DUE_OUTLINE_REQUESTS";
    public const string CheckReadyAnalysisContentsToOutlineTriggered = "CHECK_READY_ANALYSIS_CONTENTS_TO_OUTLINE";
    public const string CheckReadyAnalysisContentsToResultTriggered = "CHECK_READY_ANALYSIS_CONTENTS_TO_RESULT";
}
