namespace Hhs.TextNormalizerService.MockApis;

public sealed class MockOpenAiOutlineApi
{
    public async Task<string> GenerateOutlineAsync(string inputText, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);

        var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"[OpenAI] Point {i + 1}: {line.Trim()}"));
        return outline;
    }
}
