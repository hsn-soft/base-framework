namespace Hhs.TextNormalizerService.Models;

public sealed class ManualScrapingInput
{
    public string? CorrelationId { get; set; }
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
}