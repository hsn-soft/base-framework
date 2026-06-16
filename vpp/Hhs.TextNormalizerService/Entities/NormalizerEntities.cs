using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.TextNormalizerService.Entities;

public sealed class CustomerContentNormalizedRequest
{
    [BsonId]
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }

    public Guid CustomerContentId { get; set; }
    public string Url { get; set; } = default!;

    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    public string? ScrapingStatus { get; set; }
    public ScrapingResult? ScrapingResult { get; set; }

    public string? OutlineStatus { get; set; }
    public OutlineResult? OutlineResult { get; set; }

    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public string OutlineProviderKey { get; set; } = default!;
    public string VideoProviderKey { get; set; } = default!;
    public string? AudioProviderKey { get; set; }

    public string? OutlineProviderTrackId { get; set; }
    public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = 60;
}

public sealed class AnalysisContentNormalizedRequest
{
    [BsonId]
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }

    public Guid AnalysisContentId { get; set; }

    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    public List<AnalysisNormalizedItem> Items { get; set; } = [];

    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public string OutlineProviderKey { get; set; } = default!;
    public string VideoProviderKey { get; set; } = default!;
    public string? AudioProviderKey { get; set; }


}

public sealed class AnalysisNormalizedItem
{
    public Guid CustomerContentId { get; set; }
    public int SortOrder { get; set; }
    public string Url { get; set; } = default!;
    public string? Path { get; set; }

    public string ScrapingStatus { get; set; } = "CREATED";
    public ScrapingResult? ScrapingResult { get; set; }

    public string OutlineStatus { get; set; } = "CREATED";
    public OutlineResult? OutlineResult { get; set; }

    public string? LastError { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public string? OutlineProviderTrackId { get; set; }
    public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = 60;
}

public sealed class ScrapingResult
{
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
    public string Source { get; set; } = "AUTO";
}

public sealed class OutlineResult
{
    public string Script { get; set; } = default!;
    public string Source { get; set; } = "AUTO";
}

public sealed class NormalizerInboxMessage
{
    [BsonId]
    public Guid EventId { get; set; }

    public string EventName { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string Status { get; set; } = "STARTED";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}