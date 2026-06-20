namespace Hhs.Shared.Events;

public static class StatusNames
{
    // Generic statuses
    public const string Created = "CREATED";
    public const string Started = "STARTED";
    public const string Completed = "COMPLETED";
    public const string Processing = "PROCESSING";
    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";

    // Scraping statuses
    public const string Scraping = "SCRAPING";
    public const string ScrapingCompleted = "SCRAPING_COMPLETED";
    public const string ScrapingPartiallyCompleted = "SCRAPING_PARTIALLY_COMPLETED";
    public const string WaitingScraping = "WAITING_SCRAPING";

    // Outline provider statuses
    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderPolling = "OUTLINE_PROVIDER_POLLING";
    public const string OutlineProviderCompleted = "OUTLINE_PROVIDER_COMPLETED";
    public const string ProviderCompleted = "PROVIDER_COMPLETED";

    // Outline statuses
    public const string Outline = "OUTLINE";
    public const string OutlineCompleted = "OUTLINE_COMPLETED";
    public const string OutlinePartiallyCompleted = "OUTLINE_PARTIALLY_COMPLETED";

    public const string NotStarted = "NOT_STARTED";
    public const string Approved = "APPROVED";
    public const string AudioProviderPolling = "AUDIO_PROVIDER_POLLING";
    public const string AudioProviderCompleted = "AUDIO_PROVIDER_COMPLETED";
    public const string AudioProviderRequestStarted = "AUDIO_PROVIDER_REQUEST_STARTED";
    public const string Downloading = "DOWNLOADING";

    // Audio/Video request statuses
    public const string AudioRequestCreated = "AUDIO_REQUEST_CREATED";
    public const string AudioProviderRequestRetrying = "AUDIO_PROVIDER_REQUEST_RETRYING";
    public const string Downloaded = "DOWNLOADED";
    public const string Uploading = "UPLOADING";
    public const string Uploaded = "UPLOADED";

    // Video provider statuses
    public const string VideoProviderRequestStarting = "VIDEO_PROVIDER_REQUEST_STARTING";
    public const string VideoProviderRequestStarted = "VIDEO_PROVIDER_REQUEST_STARTED";
    public const string VideoProviderPolling = "VIDEO_PROVIDER_POLLING";
    public const string VideoProviderCompleted = "VIDEO_PROVIDER_COMPLETED";
    public const string VideoDownloading = "VIDEO_DOWNLOADING";
    public const string VideoDownloaded = "VIDEO_DOWNLOADED";
    public const string VideoUploading = "VIDEO_UPLOADING";

    // Retry statuses
    public const string RetryEventPublished = "RETRY_EVENT_PUBLISHED";

    // Polling statuses
    public const string Polling = "POLLING";
}
