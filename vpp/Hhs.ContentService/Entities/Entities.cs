using Hhs.Shared.Inbox;

namespace Hhs.ContentService.Entities;

public sealed class CustomerContent
{
    public Guid Id { get; set; }
    public string Url { get; set; } = default!;
    public string? Title { get; set; }
    public string? Path { get; set; }

    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }
    public Guid? AudioRequestId { get; set; }

    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }


    public string OutlineProviderKey { get; set; } = "openai";
    public string VideoProviderKey { get; set; } = "video-a";
    public string? AudioProviderKey { get; set; } = "audio-a";
}

public sealed class AnalysisContent
{
    public Guid Id { get; set; }
    public string? Title { get; set; }

    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }

    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    public List<AnalysisContentItem> Items { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }


    public string OutlineProviderKey { get; set; } = "openai";
    public string VideoProviderKey { get; set; } = "video-a";
    public string? AudioProviderKey { get; set; } = "audio-a";
}

public sealed class AnalysisContentItem
{
    public Guid Id { get; set; }
    public Guid AnalysisContentId { get; set; }
    public AnalysisContent AnalysisContent { get; set; } = default!;

    public Guid CustomerContentId { get; set; }
    public CustomerContent CustomerContent { get; set; } = default!;

    public int SortOrder { get; set; }
}

public sealed class ContentInboxMessage
{
    public Guid EventId { get; set; }

    public string EventName { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string Status { get; set; } = InboxStatuses.Started;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}