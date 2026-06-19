namespace Hhs.TextNormalizerService.Models;

public sealed class ManualScrapingInput
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
}