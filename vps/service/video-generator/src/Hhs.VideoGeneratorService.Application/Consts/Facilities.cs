namespace Hhs.VideoGeneratorService.Application.Consts;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents. Deliberately separate from EventNames (which names the actual integration events
/// published on the bus) — a facility describes what's happening right now, not which event fired.
/// EventNames stays in use for retry-mechanism bookkeeping (CurrentStep/FailedStep values).
/// </summary>
public static class Facilities
{
    public const string VideoRequestCreated = "VIDEO_REQUEST_CREATED";
    public const string VideoOperationStarted = "VIDEO_OPERATION_STARTED";
    public const string VideoAudioInternal = "VIDEO_AUDIO_INTERNAL";
    public const string VideoAudioExternal = "VIDEO_AUDIO_EXTERNAL";
    public const string AudioRequestCreated = "AUDIO_REQUEST_CREATED";
    public const string AudioProviderRequestStarted = "AUDIO_PROVIDER_REQUEST_STARTED";
    public const string AudioProviderCompleted = "AUDIO_PROVIDER_COMPLETED";
    public const string AudioProviderPollingStarted = "AUDIO_PROVIDER_POLLING_STARTED";
    public const string AudioFileDownloadStarted = "AUDIO_FILE_DOWNLOAD_STARTED";
    public const string AudioFileDownloadCompleted = "AUDIO_FILE_DOWNLOAD_COMPLETED";
    public const string AudioFileUploadStarted = "AUDIO_FILE_UPLOAD_STARTED";
    public const string AudioFileUploadCompleted = "AUDIO_FILE_UPLOAD_COMPLETED";
    public const string VideoProviderRequestStarted = "VIDEO_PROVIDER_REQUEST_STARTED";
    public const string VideoProviderCompleted = "VIDEO_PROVIDER_COMPLETED";
    public const string VideoProviderPollingStarted = "VIDEO_PROVIDER_POLLING_STARTED";
    public const string VideoFileDownloadStarted = "VIDEO_FILE_DOWNLOAD_STARTED";
    public const string VideoFileDownloadCompleted = "VIDEO_FILE_DOWNLOAD_COMPLETED";
    public const string VideoFileUploadStarted = "VIDEO_FILE_UPLOAD_STARTED";
    public const string VideoFileUploadCompleted = "VIDEO_FILE_UPLOAD_COMPLETED";
    public const string VideoGenerationResultPublished = "VIDEO_GENERATION_RESULT_PUBLISHED";
    public const string RetryScheduled = "RETRY_SCHEDULED";
    public const string StepFailed = "STEP_FAILED";

    public const string RetryDueRequestsTriggered = "RETRY_DUE_REQUESTS";
    public const string PollDueVideoRequestsTriggered = "POLL_DUE_VIDEO_REQUESTS";
    public const string PollDueAudioRequestsTriggered = "POLL_DUE_AUDIO_REQUESTS";
}
