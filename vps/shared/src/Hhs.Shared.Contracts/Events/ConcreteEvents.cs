using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events;

public sealed record CustomerContentCreatedEto : IIntegrationEventMessage
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public string ContentKey { get; init; } = default!;
    public Guid CustomerContentId { get; init; }
}

public sealed record AnalysisContentCreatedEto : IIntegrationEventMessage
{
    public string ScopeKey { get; init; } = default!;
    public string DomainName { get; init; } = default!;
    public List<AnalysisNormalizeItem> Items { get; init; } = [];
    public Guid AnalysisContentId { get; init; }
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
    public Guid CustomerContentNormalizeRequestId { get; init; }
}

public sealed record AnalysisContentNormalizeRequestCreatedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid NormalizeRequestId { get; init; }
}

public sealed record CustomerContentScrapingStartedEto : IIntegrationEventMessage
{
    public Guid CustomerContentNormalizeRequestId { get; init; }
}

public sealed record CustomerContentScrapingCompletedEto : IIntegrationEventMessage
{
    public Guid CustomerContentNormalizeRequestId { get; init; }
}

public sealed record CustomerContentOutlineStartedEto : IIntegrationEventMessage
{
    public Guid CustomerContentNormalizeRequestId { get; init; }
}

public sealed record CustomerContentOutlineCompletedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string Script { get; init; } = default!;
}

public sealed record AnalysisItemScrapingStartedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
}

public sealed record AnalysisItemScrapingCompletedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
}

public sealed record AnalysisItemOutlineStartedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }
}

public sealed record OutlineProviderRequestStartedEto : IIntegrationEventMessage
{
    public Guid RefNormalizedRequestId { get; init; }
    public ContentType RefContentType { get; init; }

    public Guid? CustomerContentIdForItem { get; init; }

    public string ScopeKey { get; init; } = default!;

    public string InputText { get; init; } = default!;
    public string InputPrompt { get; init; } = default!;
}

public sealed record OutlineProviderCompletedEto : IIntegrationEventMessage
{
    public ContentType RefContentType { get; init; }

    public Guid RefNormalizedRequestId { get; init; }
    public Guid? CustomerContentIdForItem { get; init; }

    public string OutlinedData { get; init; } = default!;
}

public sealed record AnalysisItemOutlineCompletedEto : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; init; }
}

public sealed record NormalizerResultPublishedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public Guid NormalizeRequestId { get; init; }
}

public sealed record VideoGenerationApprovedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid RefNormalizeRequestId { get; init; }
    public string ScopeKey { get; init; } = default!;
}

public sealed record VideoGenerationDataForwardedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public string ScopeKey { get; init; } = default!;
    public Guid NormalizeRequestId { get; init; }
    public string VideoInputJson { get; init; } = default!;
}

public sealed record VideoRequestCreatedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoOperationStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record AudioProviderRequestStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }
}

public sealed record AudioProviderPollingStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }
}

public sealed record AudioProviderCompletedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }
}

public sealed record AudioFileDownloadStartedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }
}

public sealed record AudioFileDownloadCompletedEto : IIntegrationEventMessage
{
    public Guid AudioRequestId { get; init; }
}

public sealed record AudioFileUploadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoProviderRequestStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
    public List<string> AudioUrls { get; init; } = [];
}

public sealed record VideoProviderPollingStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoProviderCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoFileDownloadStartedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoFileDownloadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoFileUploadCompletedEto : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; init; }
}

public sealed record VideoGenerationResultPublishedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }
    public Guid VideoRequestId { get; init; }
    [CanBeNull] public string FinalVideoUrl { get; init; } = default!;
}

public sealed record StepFailedEto : IIntegrationEventMessage
{
    public Guid RefContentId { get; init; }
    public ContentType RefContentType { get; init; }

    public string Step { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
    public bool Retryable { get; init; }
}