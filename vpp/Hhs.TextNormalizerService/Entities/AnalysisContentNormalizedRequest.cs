using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.TextNormalizerService.Entities;

public sealed class AnalysisContentNormalizedRequest
{
    // Identity & Correlation
    [BsonId]
    public Guid Id { get; set; }
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }

    // Subscription & Scope
    public string ScopeKey { get; set; } = default!;

    // Content Reference
    public Guid AnalysisContentId { get; set; }
    public string DomainName { get; set; } = default!;

    // Status & Progress
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Analysis Items
    public List<AnalysisNormalizedItem> Items { get; set; } = [];

    // Error Handling
    public string? LastError { get; set; }

    // Audit Fields
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Provider Configuration
    public string OutlineProviderKey { get; set; } = default!;
    public string VideoProviderKey { get; set; } = default!;
    public string? AudioProviderKey { get; set; }
}
