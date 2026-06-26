namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;

public sealed class OutlineResult
{
    // Content
    public string OutlinedData { get; set; } = default!;

    // Metadata
    public string Source { get; set; } = "AUTO";
}
