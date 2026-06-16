using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineFastProvider : IOutlineProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderKey => "outline-fast";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public OutlineFastProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5040/outline/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var script = json.GetProperty("script").GetString();

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
        throw new NotSupportedException("OpenAI outline provider does not support polling.");
    }
}