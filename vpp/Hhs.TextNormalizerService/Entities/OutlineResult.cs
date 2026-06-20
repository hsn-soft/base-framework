namespace Hhs.TextNormalizerService.Entities;

public sealed class OutlineResult
{
    // Content
    public string Script { get; set; } = default!;

    // Metadata
    public string Source { get; set; } = "AUTO";
}
