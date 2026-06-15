namespace Hhs.TextNormalizerService.Providers;

public interface IOutlineProvider
{
    Task<string> CreateOutlineAsync(string text, CancellationToken cancellationToken);
}

public sealed class DummyOutlineProvider : IOutlineProvider
{
    public Task<string> CreateOutlineAsync(string text, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Dummy video script from text: {text[..Math.Min(text.Length, 80)]}");
    }
}