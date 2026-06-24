namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;

public sealed class ScrapingResult
{
    // Content
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;

    // Metadata
    public DateTime? ReleaseTimeUtc { get; set; }
    public string Source { get; set; } = "AUTO";
}
