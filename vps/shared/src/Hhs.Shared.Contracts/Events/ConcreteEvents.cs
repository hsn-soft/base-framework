using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events;

public sealed record CustomerContentCreatedEto : IIntegrationEventMessage
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public string ContentKey { get; init; } = default!;
    public Guid CustomerContentId { get; init; }

    public CustomerContentCreatedEto()
    {
        // EventName = EventNames.CustomerContentCreated;
        // Facility = EventNames.CustomerContentCreated;
    }
}

public sealed record AnalysisContentCreatedEto : IIntegrationEventMessage
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public List<AnalysisNormalizeItem> Items { get; init; } = [];
    public Guid AnalysisContentId { get; init; }

    public AnalysisContentCreatedEto()
    {
        // EventName = EventNames.AnalysisContentCreated;
        // Facility = EventNames.AnalysisContentCreated;
    }
}

public sealed record AnalysisNormalizeItem
{
    public Guid CustomerContentId { get; init; }
    public int SortOrder { get; init; }
    public string ContentKey { get; init; } = default!;
}

public sealed record CustomerContentNormalizeRequestCreatedEto : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentNormalizeRequestCreatedEto()
    {
        // EventName = EventNames.CustomerContentNormalizeRequestCreated;
        // Facility = EventNames.CustomerContentNormalizeRequestCreated;
    }
}

public sealed record AnalysisContentNormalizeRequestCreatedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }

    public AnalysisContentNormalizeRequestCreatedEto()
    {
        // EventName = EventNames.AnalysisContentNormalizeRequestCreated;
        // Facility = EventNames.AnalysisContentNormalizeRequestCreated;
    }
}

public sealed record CustomerContentScrapingStartedEto : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentScrapingStartedEto()
    {
        // EventName = EventNames.CustomerContentScrapingStarted;
        // Facility = EventNames.CustomerContentScrapingStarted;
    }
}

public sealed record CustomerContentScrapingCompletedEto : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentScrapingCompletedEto()
    {
        // EventName = EventNames.CustomerContentScrapingCompleted;
        // Facility = EventNames.CustomerContentScrapingCompleted;
    }
}

public sealed record CustomerContentOutlineStartedEto : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; init; }

    public CustomerContentOutlineStartedEto()
    {
        // EventName = EventNames.CustomerContentOutlineStarted;
        // Facility = EventNames.CustomerContentOutlineStarted;
    }
}

public sealed record CustomerContentOutlineCompletedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }


    public string Script { get; init; } = default!;

    public CustomerContentOutlineCompletedEto()
    {
        // EventName = EventNames.CustomerContentOutlineCompleted;
        // Facility = EventNames.CustomerContentOutlineCompleted;
    }
}

public sealed record AnalysisItemScrapingStartedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemScrapingStartedEto()
    {
        // EventName = EventNames.AnalysisItemScrapingStarted;
        // Facility = EventNames.AnalysisItemScrapingStarted;
    }
}

public sealed record AnalysisItemScrapingCompletedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemScrapingCompletedEto()
    {
        // EventName = EventNames.AnalysisItemScrapingCompleted;
        // Facility = EventNames.AnalysisItemScrapingCompleted;
    }
}

public sealed record AnalysisItemOutlineStartedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public AnalysisItemOutlineStartedEto()
    {
        // EventName = EventNames.AnalysisItemOutlineStarted;
        // Facility = EventNames.AnalysisItemOutlineStarted;
    }
}

public sealed record OutlineProviderRequestStartedEto : IIntegrationEventMessage
{
    public ContentType RefContentType { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
    public Guid NormalizedRequestId { get; init; }

    public string ScopeKey { get; init; } = default!;


    public string InputText { get; init; } = default!;

    public OutlineProviderRequestStartedEto()
    {
        // EventName = EventNames.OutlineProviderRequestStarted;
        // Facility = EventNames.OutlineProviderRequestStarted;
    }
}

public sealed record OutlineProviderCompletedEto : IIntegrationEventMessage
{
    public ContentType RefContentType { get; init; }

    public Guid NormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public string Script { get; init; } = default!;

    public OutlineProviderCompletedEto()
    {
        // EventName = EventNames.OutlineProviderCompleted;
        // Facility = EventNames.OutlineProviderCompleted;
    }
}

public sealed record AnalysisItemOutlineCompletedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }

    public AnalysisItemOutlineCompletedEto()
    {
        // EventName = EventNames.AnalysisItemOutlineCompleted;
        // Facility = EventNames.AnalysisItemOutlineCompleted;
    }
}

public sealed record NormalizerResultPublishedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public Guid NormalizeRequestId { get; init; }
    public string VideoInputJson { get; init; } = default!;

    public NormalizerResultPublishedEto()
    {
        // EventName = EventNames.NormalizerResultPublished;
        // Facility = EventNames.NormalizerResultPublished;
    }
}

public sealed record VideoGenerationApprovedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string ScopeKey { get; init; } = default!;
    public string VideoInputJson { get; init; } = default!;

    public VideoGenerationApprovedEto()
    {
        // EventName = EventNames.VideoGenerationApproved;
        // Facility = EventNames.VideoGenerationApproved;
    }
}

public sealed record VideoRequestCreatedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }

    public VideoRequestCreatedEto()
    {
        // EventName = EventNames.VideoRequestCreated;
        // Facility = EventNames.VideoRequestCreated;
    }
}

public sealed record VideoOperationStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoOperationStartedEto()
    {
        // EventName = EventNames.VideoOperationStarted;
        // Facility = EventNames.VideoOperationStarted;
    }
}

public sealed record AudioProviderRequestStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderRequestStartedEto()
    {
        // EventName = EventNames.AudioProviderRequestStarted;
        // Facility = EventNames.AudioProviderRequestStarted;
    }
}

public sealed record AudioProviderPollingStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderPollingStartedEto()
    {
        // EventName = EventNames.AudioProviderPollingStarted;
        // Facility = EventNames.AudioProviderPollingStarted;
    }
}

public sealed record AudioProviderCompletedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }

    public AudioProviderCompletedEto()
    {
        // EventName = EventNames.AudioProviderCompleted;
        // Facility = EventNames.AudioProviderCompleted;
    }
}

public sealed record AudioFileDownloadStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }

    public AudioFileDownloadStartedEto()
    {
        // EventName = EventNames.AudioFileDownloadStarted;
        // Facility = EventNames.AudioFileDownloadStarted;
    }
}

public sealed record AudioFileDownloadCompletedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }

    public AudioFileDownloadCompletedEto()
    {
        // EventName = EventNames.AudioFileDownloadCompleted;
        // Facility = EventNames.AudioFileDownloadCompleted;
    }
}

public sealed record AudioFileUploadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public AudioFileUploadCompletedEto()
    {
        // EventName = EventNames.AudioFileUploadCompleted;
        // Facility = EventNames.AudioFileUploadCompleted;
    }
}

public sealed record VideoProviderRequestStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
    public List<string> AudioUrls { get; init; } = [];

    public VideoProviderRequestStartedEto()
    {
        // EventName = EventNames.VideoProviderRequestStarted;
        // Facility = EventNames.VideoProviderRequestStarted;
    }
}

public sealed record VideoProviderPollingStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoProviderPollingStartedEto()
    {
        // EventName = EventNames.VideoProviderPollingStarted;
        // Facility = EventNames.VideoProviderPollingStarted;
    }
}

public sealed record VideoProviderCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoProviderCompletedEto()
    {
        // EventName = EventNames.VideoProviderCompleted;
        // Facility = EventNames.VideoProviderCompleted;
    }
}

public sealed record VideoFileDownloadStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoFileDownloadStartedEto()
    {
        // EventName = EventNames.VideoFileDownloadStarted;
        // Facility = EventNames.VideoFileDownloadStarted;
    }
}

public sealed record VideoFileDownloadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoFileDownloadCompletedEto()
    {
        // EventName = EventNames.VideoFileDownloadCompleted;
        // Facility = EventNames.VideoFileDownloadCompleted;
    }
}

public sealed record VideoFileUploadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }

    public VideoFileUploadCompletedEto()
    {
        // EventName = EventNames.VideoFileUploadCompleted;
        // Facility = EventNames.VideoFileUploadCompleted;
    }
}

public sealed record VideoGenerationResultPublishedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }
    public string? FinalVideoUrl { get; init; } = default!;

    public VideoGenerationResultPublishedEto()
    {
        // EventName = EventNames.VideoGenerationResultPublished;
        // Facility = EventNames.VideoGenerationResultPublished;
    }
}

public sealed record StepFailedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string Step { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
    public bool Retryable { get; init; }

    public StepFailedEto()
    {
        // EventName = EventNames.StepFailed;
        // Facility = EventNames.StepFailed;
    }
}