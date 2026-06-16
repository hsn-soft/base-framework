namespace Hhs.TextNormalizerService.Models;

public sealed record DemoOutlineRequest
{
    public string Text { get; init; } = default!;
}
