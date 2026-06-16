namespace Hhs.Shared.Events;

public static class ContentProcessTypes
{
    public const string CustomerContent = "CUSTOMER_CONTENT";
    public const string AnalysisContent = "ANALYSIS_CONTENT";
}

public static class EventNames
{
    public const string CustomerNormalizeRequestCreated = "CUSTOMER_NORMALIZE_REQUEST_CREATED";
    public const string AnalysisNormalizeRequestCreated = "ANALYSIS_NORMALIZE_REQUEST_CREATED";

    public const string CustomerScrapingStarted = "CUSTOMER_SCRAPING_STARTED";
    public const string CustomerScrapingCompleted = "CUSTOMER_SCRAPING_COMPLETED";
    public const string CustomerOutlineStarted = "CUSTOMER_OUTLINE_STARTED";

    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderPollingStarted = "OUTLINE_PROVIDER_POLLING_STARTED";
    public const string OutlineProviderCompleted = "OUTLINE_PROVIDER_COMPLETED";

    public const string CustomerOutlineCompleted = "CUSTOMER_OUTLINE_COMPLETED";

    public const string AnalysisItemScrapingStarted = "ANALYSIS_ITEM_SCRAPING_STARTED";
    public const string AnalysisItemScrapingCompleted = "ANALYSIS_ITEM_SCRAPING_COMPLETED";
    public const string AnalysisItemOutlineStarted = "ANALYSIS_ITEM_OUTLINE_STARTED";
    public const string AnalysisItemOutlineCompleted = "ANALYSIS_ITEM_OUTLINE_COMPLETED";

    public const string NormalizerResultPublished = "NORMALIZER_RESULT_PUBLISHED";
    public const string VideoGenerationApproved = "VIDEO_GENERATION_APPROVED";

    public const string VideoRequestCreated = "VIDEO_REQUEST_CREATED";
    public const string VideoOperationStarted = "VIDEO_OPERATION_STARTED";

    public const string AudioProviderRequestStarted = "AUDIO_PROVIDER_REQUEST_STARTED";
    public const string AudioProviderPollingStarted = "AUDIO_PROVIDER_POLLING_STARTED";
    public const string AudioProviderCompleted = "AUDIO_PROVIDER_COMPLETED";
    public const string AudioFileDownloadStarted = "AUDIO_FILE_DOWNLOAD_STARTED";
    public const string AudioFileDownloadCompleted = "AUDIO_FILE_DOWNLOAD_COMPLETED";
    public const string AudioFileUploadStarted = "AUDIO_FILE_UPLOAD_STARTED";
    public const string AudioFileUploadCompleted = "AUDIO_FILE_UPLOAD_COMPLETED";

    public const string VideoProviderRequestStarted = "VIDEO_PROVIDER_REQUEST_STARTED";
    public const string VideoProviderPollingStarted = "VIDEO_PROVIDER_POLLING_STARTED";
    public const string VideoProviderCompleted = "VIDEO_PROVIDER_COMPLETED";
    public const string VideoFileDownloadStarted = "VIDEO_FILE_DOWNLOAD_STARTED";
    public const string VideoFileDownloadCompleted = "VIDEO_FILE_DOWNLOAD_COMPLETED";
    public const string VideoFileUploadStarted = "VIDEO_FILE_UPLOAD_STARTED";
    public const string VideoGenerationResultPublished = "VIDEO_GENERATION_RESULT_PUBLISHED";

    public const string StepFailed = "STEP_FAILED";
    public const string RetryScheduled = "RETRY_SCHEDULED";
}