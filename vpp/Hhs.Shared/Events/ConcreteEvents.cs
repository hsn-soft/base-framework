namespace Hhs.Shared.Events;

public sealed record CustomerNormalizeRequestCreatedEvent : IntegrationEvent
{
    public string Url { get; init; } = default!;

    public CustomerNormalizeRequestCreatedEvent()
    {
        EventName = EventNames.CustomerNormalizeRequestCreated;
        Facility = EventNames.CustomerNormalizeRequestCreated;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }

    public string OutlineProviderKey { get; init; } = default!;
    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record AnalysisNormalizeRequestCreatedEvent : IntegrationEvent
{
    public List<AnalysisNormalizeItem> Items { get; init; } = [];

    public AnalysisNormalizeRequestCreatedEvent()
    {
        EventName = EventNames.AnalysisNormalizeRequestCreated;
        Facility = EventNames.AnalysisNormalizeRequestCreated;
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

public sealed record CustomerScrapingStartedEvent : IntegrationEvent
{
    public CustomerScrapingStartedEvent()
    {
        EventName = EventNames.CustomerScrapingStarted;
        Facility = EventNames.CustomerScrapingStarted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerScrapingCompletedEvent : IntegrationEvent
{
    public string Title { get; init; } = default!;
    public string Text { get; init; } = default!;
    public DateTime? ReleaseTimeUtc { get; init; }

    public CustomerScrapingCompletedEvent()
    {
        EventName = EventNames.CustomerScrapingCompleted;
        Facility = EventNames.CustomerScrapingCompleted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerOutlineStartedEvent : IntegrationEvent
{
    public CustomerOutlineStartedEvent()
    {
        EventName = EventNames.CustomerOutlineStarted;
        Facility = EventNames.CustomerOutlineStarted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record CustomerOutlineCompletedEvent : IntegrationEvent
{
    public string Script { get; init; } = default!;

    public CustomerOutlineCompletedEvent()
    {
        EventName = EventNames.CustomerOutlineCompleted;
        Facility = EventNames.CustomerOutlineCompleted;
        ContentProcessType = ContentProcessTypes.CustomerContent;
    }
}

public sealed record AnalysisItemScrapingStartedEvent : IntegrationEvent
{
    public int SortOrder { get; init; }

    public AnalysisItemScrapingStartedEvent()
    {
        EventName = EventNames.AnalysisItemScrapingStarted;
        Facility = EventNames.AnalysisItemScrapingStarted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record AnalysisItemScrapingCompletedEvent : IntegrationEvent
{
    public int SortOrder { get; init; }
    public string Title { get; init; } = default!;
    public string Text { get; init; } = default!;
    public DateTime? ReleaseTimeUtc { get; init; }

    public AnalysisItemScrapingCompletedEvent()
    {
        EventName = EventNames.AnalysisItemScrapingCompleted;
        Facility = EventNames.AnalysisItemScrapingCompleted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record AnalysisItemOutlineStartedEvent : IntegrationEvent
{
    public int SortOrder { get; init; }

    public AnalysisItemOutlineStartedEvent()
    {
        EventName = EventNames.AnalysisItemOutlineStarted;
        Facility = EventNames.AnalysisItemOutlineStarted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record OutlineProviderRequestStartedEvent : IntegrationEvent
{
    public string ProviderKey { get; init; } = default!;
    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public int? SortOrder { get; init; }
    public string InputText { get; init; } = default!;

    public OutlineProviderRequestStartedEvent()
    {
        EventName = EventNames.OutlineProviderRequestStarted;
        Facility = EventNames.OutlineProviderRequestStarted;
    }
}

public sealed record OutlineProviderPollingStartedEvent : IntegrationEvent
{
    public string ProviderKey { get; init; } = default!;
    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public int? SortOrder { get; init; }
    public string ProviderTrackId { get; init; } = default!;

    public OutlineProviderPollingStartedEvent()
    {
        EventName = EventNames.OutlineProviderPollingStarted;
        Facility = EventNames.OutlineProviderPollingStarted;
    }
}

public sealed record OutlineProviderCompletedEvent : IntegrationEvent
{
    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public int? SortOrder { get; init; }
    public string Script { get; init; } = default!;

    public OutlineProviderCompletedEvent()
    {
        EventName = EventNames.OutlineProviderCompleted;
        Facility = EventNames.OutlineProviderCompleted;
    }
}

public sealed record AnalysisItemOutlineCompletedEvent : IntegrationEvent
{
    public int SortOrder { get; init; }
    public string Script { get; init; } = default!;

    public AnalysisItemOutlineCompletedEvent()
    {
        EventName = EventNames.AnalysisItemOutlineCompleted;
        Facility = EventNames.AnalysisItemOutlineCompleted;
        ContentProcessType = ContentProcessTypes.AnalysisContent;
    }
}

public sealed record NormalizerResultPublishedEvent : IntegrationEvent
{
    public string VideoInputJson { get; init; } = default!;

    public NormalizerResultPublishedEvent()
    {
        EventName = EventNames.NormalizerResultPublished;
        Facility = EventNames.NormalizerResultPublished;
    }

    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record VideoGenerationApprovedEvent : IntegrationEvent
{
    public string VideoInputJson { get; init; } = default!;

    public VideoGenerationApprovedEvent()
    {
        EventName = EventNames.VideoGenerationApproved;
        Facility = EventNames.VideoGenerationApproved;
    }

    public string VideoProviderKey { get; init; } = default!;
    public string? AudioProviderKey { get; init; }
}

public sealed record VideoRequestCreatedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public bool IsAnalysis { get; init; }
    public bool ExternalAudioRequired { get; init; }

    public VideoRequestCreatedEvent()
    {
        EventName = EventNames.VideoRequestCreated;
        Facility = EventNames.VideoRequestCreated;
    }
}

public sealed record VideoOperationStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public bool ExternalAudioRequired { get; init; }

    public VideoOperationStartedEvent()
    {
        EventName = EventNames.VideoOperationStarted;
        Facility = EventNames.VideoOperationStarted;
    }
}

public sealed record AudioProviderRequestStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public int SortOrder { get; init; }
    public string InputText { get; init; } = default!;

    public AudioProviderRequestStartedEvent()
    {
        EventName = EventNames.AudioProviderRequestStarted;
        Facility = EventNames.AudioProviderRequestStarted;
    }
}

public sealed record AudioProviderPollingStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderKey { get; init; } = default!;
    public string ProviderTrackId { get; init; } = default!;

    public AudioProviderPollingStartedEvent()
    {
        EventName = EventNames.AudioProviderPollingStarted;
        Facility = EventNames.AudioProviderPollingStarted;
    }
}

public sealed record AudioProviderCompletedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;

    public AudioProviderCompletedEvent()
    {
        EventName = EventNames.AudioProviderCompleted;
        Facility = EventNames.AudioProviderCompleted;
    }
}

public sealed record AudioFileDownloadStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;

    public AudioFileDownloadStartedEvent()
    {
        EventName = EventNames.AudioFileDownloadStarted;
        Facility = EventNames.AudioFileDownloadStarted;
    }
}

public sealed record AudioFileUploadStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string LocalFilePath { get; init; } = default!;

    public AudioFileUploadStartedEvent()
    {
        EventName = EventNames.AudioFileUploadStarted;
        Facility = EventNames.AudioFileUploadStarted;
    }
}

public sealed record AudioFileUploadCompletedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public Guid AudioRequestId { get; init; }
    public string StorageUrl { get; init; } = default!;

    public AudioFileUploadCompletedEvent()
    {
        EventName = EventNames.AudioFileUploadCompleted;
        Facility = EventNames.AudioFileUploadCompleted;
    }
}

public sealed record VideoProviderRequestStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public List<string> AudioUrls { get; init; } = [];
    public List<string> AudioFilePaths { get; init; } = [];

    public VideoProviderRequestStartedEvent()
    {
        EventName = EventNames.VideoProviderRequestStarted;
        Facility = EventNames.VideoProviderRequestStarted;
    }
}

public sealed record VideoProviderPollingStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderKey { get; init; } = default!;
    public string ProviderTrackId { get; init; } = default!;

    public VideoProviderPollingStartedEvent()
    {
        EventName = EventNames.VideoProviderPollingStarted;
        Facility = EventNames.VideoProviderPollingStarted;
    }
}

public sealed record VideoProviderCompletedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;

    public VideoProviderCompletedEvent()
    {
        EventName = EventNames.VideoProviderCompleted;
        Facility = EventNames.VideoProviderCompleted;
    }
}

public sealed record VideoFileDownloadStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string ProviderFileUrl { get; init; } = default!;

    public VideoFileDownloadStartedEvent()
    {
        EventName = EventNames.VideoFileDownloadStarted;
        Facility = EventNames.VideoFileDownloadStarted;
    }
}

public sealed record VideoFileUploadStartedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string LocalFilePath { get; init; } = default!;

    public VideoFileUploadStartedEvent()
    {
        EventName = EventNames.VideoFileUploadStarted;
        Facility = EventNames.VideoFileUploadStarted;
    }
}

public sealed record VideoGenerationResultPublishedEvent : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }
    public string FinalVideoUrl { get; init; } = default!;

    public VideoGenerationResultPublishedEvent()
    {
        EventName = EventNames.VideoGenerationResultPublished;
        Facility = EventNames.VideoGenerationResultPublished;
    }
}

public sealed record StepFailedEvent : IntegrationEvent
{
    public string Step { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
    public bool Retryable { get; init; }

    public StepFailedEvent()
    {
        EventName = EventNames.StepFailed;
        Facility = EventNames.StepFailed;
    }
}