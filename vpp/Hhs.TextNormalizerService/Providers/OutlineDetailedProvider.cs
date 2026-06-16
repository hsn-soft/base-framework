using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineDetailedProvider : IOutlineProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderKey => "outline-detailed";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public OutlineDetailedProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5041/outline/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var trackingId = json.GetProperty("trackingId").GetString();

        return new OutlineCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = trackingId
        };
    }

    public async Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5041/outline/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var status = json.GetProperty("status").GetString();

        if (status != "completed")
        {
            return new OutlineStatusResponse
            {
                IsCompleted = false,
                IsFailed = false
            };
        }

        var script = json.GetProperty("script").GetString();
        return new OutlineStatusResponse
        {
            IsCompleted = true,
            IsFailed = false,
            Script = script
        };
    }
}