namespace Hhs.VideoGeneratorService.Domain.Constants;

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