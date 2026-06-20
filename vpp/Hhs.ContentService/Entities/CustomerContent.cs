namespace Hhs.ContentService.Entities;

public sealed class CustomerContent
{
    // Audit Fields
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Correlation & Tracing
    public Guid? CorrelationId { get; set; }

    // Content Metadata
    public string Url { get; set; } = default!;
    public string? Title { get; set; }
    public string? Path { get; set; }

    // Normalization Status
    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }
    public Guid? AudioRequestId { get; set; }

    // Result & Errors
    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    // Provider Configuration
    public string OutlineProviderKey { get; set; } = "openai";
    public string VideoProviderKey { get; set; } = "video-a";
    public string? AudioProviderKey { get; set; } = "audio-a";
}
