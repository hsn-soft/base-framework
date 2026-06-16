using Hhs.Shared.Providers;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineProviderAbc : IOutlineProvider
{
    public string ProviderKey => "outline-abc";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);

        var script = GenerateDummyScript(request.InputText);

        return new OutlineCreateResponse
        {
            IsCompleted = true,
            Script = script
        };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("ABC provider does not support polling.");
    }

    private static string GenerateDummyScript(string inputText)
    {
        var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"Point {i + 1}: {line.Trim()}"));
        return outline;
    }
}
