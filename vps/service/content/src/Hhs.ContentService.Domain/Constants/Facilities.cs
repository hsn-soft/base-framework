namespace Hhs.ContentService.Domain.Constants;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents. Deliberately separate from Milestones (which names the actual integration events
/// published on the bus) — a facility describes what's happening right now, not which event fired.
/// Lives in Domain (not Application) so entity constructors and EF Core repositories can use it too.
/// </summary>
public static class Facilities
{
    public const string NormalizeRequestReferenceSet = "NORMALIZE_REQUEST_REFERENCE_SET";
    public const string CustomerContentScrapingCompleted = "CUSTOMER_CONTENT_SCRAPING_COMPLETED";
    public const string ContentOutlineRejected = "CONTENT_OUTLINE_REJECTED";
    public const string ContentNormalizedSuccess = "CONTENT_NORMALIZED_SUCCESS";
    public const string VideoGenerationApproved = "VIDEO_GENERATION_APPROVED";
    public const string VideoGenerationRejected = "VIDEO_GENERATION_REJECTED";
    public const string VideoRequestCreated = "VIDEO_REQUEST_CREATED";
    public const string AudioOperationStarted = "AUDIO_OPERATION_STARTED";
    public const string VideoProviderRequestStarted = "VIDEO_PROVIDER_REQUEST_STARTED";
    public const string VideoGenerationResultPublished = "VIDEO_GENERATION_RESULT_PUBLISHED";
    public const string MilestoneFailed = "MILESTONE_FAILED";
    public const string AnalysisContentCreated = "ANALYSIS_CONTENT_CREATED";
    public const string CustomerContentCreated = "CUSTOMER_CONTENT_CREATED";
    public const string RetryScheduled = "RETRY_SCHEDULED";

    public const string AnalysisVideoGenerationQueryTriggered = "ANALYSIS_VIDEO_GENERATION_QUERY";
    public const string TrendVideoGenerationQueryTriggered = "TREND_VIDEO_GENERATION_QUERY";
    public const string TestQueryTriggered = "TEST_QUERY";
    public const string RetryDueRequestsTriggered = "RETRY_DUE_REQUESTS";
}
