namespace Hhs.VideoGeneratorService.Domain.Constants;

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