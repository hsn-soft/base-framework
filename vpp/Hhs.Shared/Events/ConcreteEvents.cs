namespace Hhs.Shared.Events;

public sealed record CustomerContentCreatedEto : IntegrationEvent
{
    public string Url { get; init; } = default!;

    public CustomerContentCreatedEto()
    {
        EventName = EventNames.CustomerContentCreated;
        Facility = EventNames.CustomerContentCreated;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }

    public string OutlineProviderKey { get; init; } = default!;
    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record AnalysisContentCreatedEto : IntegrationEvent
{
    public List<AnalysisNormalizeItem> Items { get; init; } = [];

    public AnalysisContentCreatedEto()
    {
        EventName = EventNames.AnalysisContentCreated;
        Facility = EventNames.AnalysisContentCreated;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }

    public string OutlineProviderKey { get; init; } = default!;
    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record AnalysisNormalizeItem
{
    public Guid CustomerContentId { get; init; }
    public int SortOrder { get; init; }
    public string Url { get; init; } = default!;
    public string? Path { get; init; }
}

public sealed record CustomerContentNormalizeRequestCreatedEto : IntegrationEvent
{
    public CustomerContentNormalizeRequestCreatedEto()
    {
        EventName = EventNames.CustomerContentNormalizeRequestCreated;
        Facility = EventNames.CustomerContentNormalizeRequestCreated;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record AnalysisContentNormalizeRequestCreatedEto : IntegrationEvent
{
    public AnalysisContentNormalizeRequestCreatedEto()
    {
        EventName = EventNames.AnalysisContentNormalizeRequestCreated;
        Facility = EventNames.AnalysisContentNormalizeRequestCreated;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record CustomerContentScrapingStartedEto : IntegrationEvent
{
    public CustomerContentScrapingStartedEto()
    {
        EventName = EventNames.CustomerContentScrapingStarted;
        Facility = EventNames.CustomerContentScrapingStarted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerContentScrapingCompletedEto : IntegrationEvent
{
    public string Title { get; init; } = default!;
    public string Text { get; init; } = default!;
    public DateTime? ReleaseTimeUtc { get; init; }

    public CustomerContentScrapingCompletedEto()
    {
        EventName = EventNames.CustomerContentScrapingCompleted;
        Facility = EventNames.CustomerContentScrapingCompleted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerContentOutlineStartedEto : IntegrationEvent
{
    public CustomerContentOutlineStartedEto()
    {
        EventName = EventNames.CustomerContentOutlineStarted;
        Facility = EventNames.CustomerContentOutlineStarted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerContentOutlineCompletedEto : IntegrationEvent
{
    public string Script { get; init; } = default!;

    public CustomerContentOutlineCompletedEto()
    {
        EventName = EventNames.CustomerContentOutlineCompleted;
        Facility = EventNames.CustomerContentOutlineCompleted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record AnalysisItemScrapingStartedEto : IntegrationEvent
{
    public int SortOrder { get; init; }

    public AnalysisItemScrapingStartedEto()
    {
        EventName = EventNames.AnalysisItemScrapingStarted;
        Facility = EventNames.AnalysisItemScrapingStarted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record AnalysisItemScrapingCompletedEto : IntegrationEvent
{
    public int SortOrder { get; init; }
    public string Title { get; init; } = default!;
    public string Text { get; init; } = default!;
    public DateTime? ReleaseTimeUtc { get; init; }

    public AnalysisItemScrapingCompletedEto()
    {
        EventName = EventNames.AnalysisItemScrapingCompleted;
        Facility = EventNames.AnalysisItemScrapingCompleted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record AnalysisItemOutlineStartedEto : IntegrationEvent
{
    public int SortOrder { get; init; }

    public AnalysisItemOutlineStartedEto()
    {
        EventName = EventNames.AnalysisItemOutlineStarted;
        Facility = EventNames.AnalysisItemOutlineStarted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record OutlineProviderRequestStartedEto : IntegrationEvent
{
    public string ProviderKey { get; init; } = default!;
    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public int? SortOrder { get; init; }
    public string InputText { get; init; } = default!;

    public OutlineProviderRequestStartedEto()
    {
        EventName = EventNames.OutlineProviderRequestStarted;
        Facility = EventNames.OutlineProviderRequestStarted;
    }
}

public sealed record OutlineProviderCompletedEto : IntegrationEvent
{
    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public int? SortOrder { get; init; }
    public string Script { get; init; } = default!;

    public OutlineProviderCompletedEto()
    {
        EventName = EventNames.OutlineProviderCompleted;
        Facility = EventNames.OutlineProviderCompleted;
    }
}

public sealed record AnalysisItemOutlineCompletedEto : IntegrationEvent
{
    public int SortOrder { get; init; }
    public string Script { get; init; } = default!;

    public AnalysisItemOutlineCompletedEto()
    {
        EventName = EventNames.AnalysisItemOutlineCompleted;
        Facility = EventNames.AnalysisItemOutlineCompleted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record NormalizerResultPublishedEto : IntegrationEvent
{
    public Guid NormalizeRequestId { get; init; }
    public string VideoInputJson { get; init; } = default!;

    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }

    public NormalizerResultPublishedEto()
    {
        EventName = EventNames.NormalizerResultPublished;
        Facility = EventNames.NormalizerResultPublished;
    }
}

public sealed record VideoGenerationApprovedEto : IntegrationEvent
{
    public string VideoInputJson { get; init; } = default!;

    public VideoGenerationApprovedEto()
    {
        EventName = EventNames.VideoGenerationApproved;
        Facility = EventNames.VideoGenerationApproved;
    }

    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record VideoRequestCreatedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public bool IsAnalysis { get; init; }
    public bool ExternalAudioRequired { get; init; }

    public VideoRequestCreatedEto()
    {
        EventName = EventNames.VideoRequestCreated;
        Facility = EventNames.VideoRequestCreated;
    }
}

public sealed record VideoOperationStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public bool ExternalAudioRequired { get; init; }

    public VideoOperationStartedEto()
    {
        EventName = EventNames.VideoOperationStarted;
        Facility = EventNames.VideoOperationStarted;
    }
}

public sealed record AudioProviderRequestStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public int SortOrder { get; init; }
    public string InputText { get; init; } = default!;

    public AudioProviderRequestStartedEto()
    {
        EventName = EventNames.AudioProviderRequestStarted;
        Facility = EventNames.AudioProviderRequestStarted;
    }
}

public sealed record AudioProviderPollingStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderKey { get; init; } = default!;
    public string ProviderTrackId { get; init; } = default!;

    public AudioProviderPollingStartedEto()
    {
        EventName = EventNames.AudioProviderPollingStarted;
        Facility = EventNames.AudioProviderPollingStarted;
    }
}

public sealed record AudioProviderCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;
    public string? FileName { get; init; }

    public AudioProviderCompletedEto()
    {
        EventName = EventNames.AudioProviderCompleted;
        Facility = EventNames.AudioProviderCompleted;
    }
}

public sealed record AudioFileDownloadStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;
    public string? FileName { get; init; }

    public AudioFileDownloadStartedEto()
    {
        EventName = EventNames.AudioFileDownloadStarted;
        Facility = EventNames.AudioFileDownloadStarted;
    }
}

public sealed record AudioFileDownloadCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string LocalFilePath { get; init; } = default!;

    public AudioFileDownloadCompletedEto()
    {
        EventName = EventNames.AudioFileDownloadCompleted;
        Facility = EventNames.AudioFileDownloadCompleted;
    }
}

public sealed record AudioFileUploadStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string LocalFilePath { get; init; } = default!;

    public AudioFileUploadStartedEto()
    {
        EventName = EventNames.AudioFileUploadStarted;
        Facility = EventNames.AudioFileUploadStarted;
    }
}

public sealed record AudioFileUploadCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string StorageUrl { get; init; } = default!;

    public AudioFileUploadCompletedEto()
    {
        EventName = EventNames.AudioFileUploadCompleted;
        Facility = EventNames.AudioFileUploadCompleted;
    }
}

public sealed record VideoProviderRequestStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public List<string> AudioUrls { get; init; } = [];
    public List<string> AudioFilePaths { get; init; } = [];

    public VideoProviderRequestStartedEto()
    {
        EventName = EventNames.VideoProviderRequestStarted;
        Facility = EventNames.VideoProviderRequestStarted;
    }
}

public sealed record VideoProviderPollingStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderKey { get; init; } = default!;
    public string ProviderTrackId { get; init; } = default!;

    public VideoProviderPollingStartedEto()
    {
        EventName = EventNames.VideoProviderPollingStarted;
        Facility = EventNames.VideoProviderPollingStarted;
    }
}

public sealed record VideoProviderCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;
    public string? FileName { get; init; }

    public VideoProviderCompletedEto()
    {
        EventName = EventNames.VideoProviderCompleted;
        Facility = EventNames.VideoProviderCompleted;
    }
}

public sealed record VideoFileDownloadStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;
    public string? FileName { get; init; }

    public VideoFileDownloadStartedEto()
    {
        EventName = EventNames.VideoFileDownloadStarted;
        Facility = EventNames.VideoFileDownloadStarted;
    }
}

public sealed record VideoFileUploadStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string LocalFilePath { get; init; } = default!;

    public VideoFileUploadStartedEto()
    {
        EventName = EventNames.VideoFileUploadStarted;
        Facility = EventNames.VideoFileUploadStarted;
    }
}

public sealed record VideoGenerationResultPublishedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string FinalVideoUrl { get; init; } = default!;

    public VideoGenerationResultPublishedEto()
    {
        EventName = EventNames.VideoGenerationResultPublished;
        Facility = EventNames.VideoGenerationResultPublished;
    }
}

public sealed record StepFailedEto : IntegrationEvent
{
    public string Step { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
    public bool Retryable { get; init; }

    public StepFailedEto()
    {
        EventName = EventNames.StepFailed;
        Facility = EventNames.StepFailed;
    }
}