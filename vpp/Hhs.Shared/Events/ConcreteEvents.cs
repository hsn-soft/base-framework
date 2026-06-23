namespace Hhs.Shared.Events;

public sealed record CustomerContentCreatedEto : IntegrationEvent
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public string ContentKey { get; init; } = default!;
    public Guid CustomerContentId { get; init; }

    public CustomerContentCreatedEto()
    {
        EventName = EventNames.CustomerContentCreated;
        Facility = EventNames.CustomerContentCreated;
    }
}

public sealed record AnalysisContentCreatedEto : IntegrationEvent
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public List<AnalysisNormalizeItem> Items { get; init; } = [];
    public Guid AnalysisContentId { get; init; }

    public AnalysisContentCreatedEto()
    {
        EventName = EventNames.AnalysisContentCreated;
        Facility = EventNames.AnalysisContentCreated;
    }
}

public sealed record AnalysisNormalizeItem
{
    public Guid CustomerContentId { get; init; }
    public int SortOrder { get; init; }
    public string ContentKey { get; init; } = default!;
}

public sealed record CustomerContentNormalizeRequestCreatedEto : IntegrationEvent
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentNormalizeRequestCreatedEto()
    {
        EventName = EventNames.CustomerContentNormalizeRequestCreated;
        Facility = EventNames.CustomerContentNormalizeRequestCreated;
    }
}

public sealed record AnalysisContentNormalizeRequestCreatedEto : IntegrationEvent
{
    public Guid AnalysisContentId { get; init; }

    public AnalysisContentNormalizeRequestCreatedEto()
    {
        EventName = EventNames.AnalysisContentNormalizeRequestCreated;
        Facility = EventNames.AnalysisContentNormalizeRequestCreated;
    }
}

public sealed record CustomerContentScrapingStartedEto : IntegrationEvent
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentScrapingStartedEto()
    {
        EventName = EventNames.CustomerContentScrapingStarted;
        Facility = EventNames.CustomerContentScrapingStarted;
    }
}

public sealed record CustomerContentScrapingCompletedEto : IntegrationEvent
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentScrapingCompletedEto()
    {
        EventName = EventNames.CustomerContentScrapingCompleted;
        Facility = EventNames.CustomerContentScrapingCompleted;
    }
}

public sealed record CustomerContentOutlineStartedEto : IntegrationEvent
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentOutlineStartedEto()
    {
        EventName = EventNames.CustomerContentOutlineStarted;
        Facility = EventNames.CustomerContentOutlineStarted;
    }
}

public sealed record CustomerContentOutlineCompletedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string Script { get; init; } = default!;

    public CustomerContentOutlineCompletedEto()
    {
        EventName = EventNames.CustomerContentOutlineCompleted;
        Facility = EventNames.CustomerContentOutlineCompleted;
    }
}

public sealed record AnalysisItemScrapingStartedEto : IntegrationEvent
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemScrapingStartedEto()
    {
        EventName = EventNames.AnalysisItemScrapingStarted;
        Facility = EventNames.AnalysisItemScrapingStarted;
    }
}

public sealed record AnalysisItemScrapingCompletedEto : IntegrationEvent
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemScrapingCompletedEto()
    {
        EventName = EventNames.AnalysisItemScrapingCompleted;
        Facility = EventNames.AnalysisItemScrapingCompleted;
    }
}

public sealed record AnalysisItemOutlineStartedEto : IntegrationEvent
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemOutlineStartedEto()
    {
        EventName = EventNames.AnalysisItemOutlineStarted;
        Facility = EventNames.AnalysisItemOutlineStarted;
    }
}

public sealed record OutlineProviderRequestStartedEto : IntegrationEvent
{
    public ContentType RefContentType { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public Guid NormalizedRequestId { get; init; }

    public string ScopeKey { get; init; } = default!;


    public string InputText { get; init; } = default!;

    public OutlineProviderRequestStartedEto()
    {
        EventName = EventNames.OutlineProviderRequestStarted;
        Facility = EventNames.OutlineProviderRequestStarted;
    }
}

public sealed record OutlineProviderCompletedEto : IntegrationEvent
{
    public ContentType RefContentType { get; init; }

    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public string Script { get; init; } = default!;

    public OutlineProviderCompletedEto()
    {
        EventName = EventNames.OutlineProviderCompleted;
        Facility = EventNames.OutlineProviderCompleted;
    }
}

public sealed record AnalysisItemOutlineCompletedEto : IntegrationEvent
{
    public Guid AnalysisContentId { get; init; }

    public AnalysisItemOutlineCompletedEto()
    {
        EventName = EventNames.AnalysisItemOutlineCompleted;
        Facility = EventNames.AnalysisItemOutlineCompleted;
    }
}

public sealed record NormalizerResultPublishedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public Guid NormalizeRequestId { get; init; }
    public string VideoInputJson { get; init; } = default!;

    public NormalizerResultPublishedEto()
    {
        EventName = EventNames.NormalizerResultPublished;
        Facility = EventNames.NormalizerResultPublished;
    }
}

public sealed record VideoGenerationApprovedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string ScopeKey { get; init; } = default!;
    public string VideoInputJson { get; init; } = default!;

    public VideoGenerationApprovedEto()
    {
        EventName = EventNames.VideoGenerationApproved;
        Facility = EventNames.VideoGenerationApproved;
    }
}

public sealed record VideoRequestCreatedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }

    public VideoRequestCreatedEto()
    {
        EventName = EventNames.VideoRequestCreated;
        Facility = EventNames.VideoRequestCreated;
    }
}

public sealed record VideoOperationStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoOperationStartedEto()
    {
        EventName = EventNames.VideoOperationStarted;
        Facility = EventNames.VideoOperationStarted;
    }
}

public sealed record AudioProviderRequestStartedEto : IntegrationEvent
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderRequestStartedEto()
    {
        EventName = EventNames.AudioProviderRequestStarted;
        Facility = EventNames.AudioProviderRequestStarted;
    }
}

public sealed record AudioProviderPollingStartedEto : IntegrationEvent
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderPollingStartedEto()
    {
        EventName = EventNames.AudioProviderPollingStarted;
        Facility = EventNames.AudioProviderPollingStarted;
    }
}

public sealed record AudioProviderCompletedEto : IntegrationEvent
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderCompletedEto()
    {
        EventName = EventNames.AudioProviderCompleted;
        Facility = EventNames.AudioProviderCompleted;
    }
}

public sealed record AudioFileDownloadStartedEto : IntegrationEvent
{
    public Guid AudioRequestId { get; init; }

    public AudioFileDownloadStartedEto()
    {
        EventName = EventNames.AudioFileDownloadStarted;
        Facility = EventNames.AudioFileDownloadStarted;
    }
}

public sealed record AudioFileDownloadCompletedEto : IntegrationEvent
{
    public Guid AudioRequestId { get; init; }

    public AudioFileDownloadCompletedEto()
    {
        EventName = EventNames.AudioFileDownloadCompleted;
        Facility = EventNames.AudioFileDownloadCompleted;
    }
}

public sealed record AudioFileUploadCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

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

    public VideoProviderRequestStartedEto()
    {
        EventName = EventNames.VideoProviderRequestStarted;
        Facility = EventNames.VideoProviderRequestStarted;
    }
}

public sealed record VideoProviderPollingStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoProviderPollingStartedEto()
    {
        EventName = EventNames.VideoProviderPollingStarted;
        Facility = EventNames.VideoProviderPollingStarted;
    }
}

public sealed record VideoProviderCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoProviderCompletedEto()
    {
        EventName = EventNames.VideoProviderCompleted;
        Facility = EventNames.VideoProviderCompleted;
    }
}

public sealed record VideoFileDownloadStartedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoFileDownloadStartedEto()
    {
        EventName = EventNames.VideoFileDownloadStarted;
        Facility = EventNames.VideoFileDownloadStarted;
    }
}

public sealed record VideoFileDownloadCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoFileDownloadCompletedEto()
    {
        EventName = EventNames.VideoFileDownloadCompleted;
        Facility = EventNames.VideoFileDownloadCompleted;
    }
}

public sealed record VideoFileUploadCompletedEto : IntegrationEvent
{
    public Guid VideoRequestId { get; init; }

    public VideoFileUploadCompletedEto()
    {
        EventName = EventNames.VideoFileUploadCompleted;
        Facility = EventNames.VideoFileUploadCompleted;
    }
}

public sealed record VideoGenerationResultPublishedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }
    public string? FinalVideoUrl { get; init; } = default!;

    public VideoGenerationResultPublishedEto()
    {
        EventName = EventNames.VideoGenerationResultPublished;
        Facility = EventNames.VideoGenerationResultPublished;
    }
}

public sealed record StepFailedEto : IntegrationEvent
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string Step { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
    public bool Retryable { get; init; }

    public StepFailedEto()
    {
        EventName = EventNames.StepFailed;
        Facility = EventNames.StepFailed;
    }
}