namespace Hhs.ContentService.Entities;

public sealed class AnalysisContent
{
    // Audit Fields
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Correlation & Tracing
    public string? CorrelationId { get; set; }

    // Subscription & Scope
    public string ScopeKey { get; set; } = default!;

    // Content Metadata
    public string DomainName { get; set; } = default!;
    public string? Title { get; set; }

    // Normalization Status
    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }

    // Result & Errors
    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    // Analysis Items
    public List<AnalysisContentItem> Items { get; set; } = [];

    // Provider Configuration
}
