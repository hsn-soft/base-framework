namespace Hhs.Shared.Helper;


public static class NormalizeStatusNames
{
    public const string NotStarted = "NOT_STARTED";
    public const string Created = "CREATED";

    public const string ScrapingStarted = "SCRAPING_STARTED";
    public const string ScrapingCompleted = "SCRAPING_COMPLETED";

    public const string OutlineStarted = "OUTLINE_STARTED";
    public const string OutlineSkipped = "OUTLINE_SKIPPED";

    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderRequestPolling = "OUTLINE_PROVIDER_REQUEST_POLLING";
    public const string OutlineProviderRequestCompleted = "OUTLINE_PROVIDER_REQUEST_COMPLETED";

    public const string OutlineCompleted = "OUTLINE_COMPLETED";

    public const string Completed = "COMPLETED";

    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";
    public const string ScrapingPartiallyCompleted = "SCRAPING_PARTIALLY_COMPLETED";
    public const string OutlinePartiallyCompleted = "OUTLINE_PARTIALLY_COMPLETED";
}

public static class ScrapingStatusNames
{
    public const string NotStarted = "NOT_STARTED";
    public const string Started = "STARTED";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";
}

public static class OutlineStatusNames
{
    public const string NotStarted = "NOT_STARTED";
    public const string Started = "STARTED";
    public const string Skipped = "SKIPPED";
    public const string Polling = "POLLING";
    public const string ProviderCompleted = "PROVIDER_COMPLETED";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string WaitingScraping = "WAITING_SCRAPING";
    public const string WaitingRetry = "WAITING_RETRY";
}

public static class MediaStatusNames
{
    public const string NotStarted = "NOT_STARTED";

    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public const string Created = "CREATED";
    public const string AudioStarted = "AUDIO_STARTED";
    public const string VideoProviderStarted = "VIDEO_PROVIDER_STARTED";
    public const string Completed = "COMPLETED";

    public const string Failed = "FAILED";
    public const string StepFailed = "STEP_FAILED";
}

// ── video-generator: AudioRequest.Status ─────────────────────────────────────
public static class AudioStatusNames
{
    public const string AudioRequestCreated = "AUDIO_REQUEST_CREATED";
    public const string AudioProviderRequestStarted = "AUDIO_PROVIDER_REQUEST_STARTED";
    public const string AudioProviderPolling = "AUDIO_PROVIDER_POLLING";
    public const string AudioProviderCompleted = "AUDIO_PROVIDER_COMPLETED";
    public const string AudioProviderRequestRetrying = "AUDIO_PROVIDER_REQUEST_RETRYING";
    public const string AudioFileDownloading = "AUDIO_FILE_DOWNLOADING";
    public const string AudioFileDownloadCompleted = "AUDIO_FILE_DOWNLOAD_COMPLETED";
    public const string AudioFileUploading = "AUDIO_FILE_UPLOADING";
    public const string AudioFileUploadCompleted = "AUDIO_FILE_UPLOAD_COMPLETED";
    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";
    public const string RetryEventPublished = "RETRY_EVENT_PUBLISHED";
}

// ── video-generator: VideoRequest.Status ─────────────────────────────────────
public static class VideoStatusNames
{
    public const string VideoProviderRequestStarting = "VIDEO_PROVIDER_REQUEST_STARTING";
    public const string VideoProviderRequestStarted = "VIDEO_PROVIDER_REQUEST_STARTED";
    public const string VideoProviderPolling = "VIDEO_PROVIDER_POLLING";
    public const string VideoProviderCompleted = "VIDEO_PROVIDER_COMPLETED";
    public const string VideoFileDownloading = "VIDEO_FILE_DOWNLOADING";
    public const string VideoFileDownloaded = "VIDEO_FILE_DOWNLOADED";
    public const string VideoFileUploading = "VIDEO_FILE_UPLOADING";
    public const string VideoFileUploadCompleted = "VIDEO_FILE_UPLOAD_COMPLETED";
    public const string Created = "CREATED";
    public const string Started = "STARTED";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";
    public const string RetryEventPublished = "RETRY_EVENT_PUBLISHED";
}
