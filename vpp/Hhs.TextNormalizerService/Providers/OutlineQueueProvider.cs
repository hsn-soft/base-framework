using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineQueueProvider : IOutlineProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public string ProviderKey => ProviderKeys.OutlineQueue;

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public OutlineQueueProvider(HttpClient httpClient, IOptions<OutlineProviderEndpointsOptions> options)
    {
        _httpClient = httpClient;
        _baseUrl = options.Value.DetailedBaseUrl;
    }

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
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
            $"{_baseUrl}/outline/status/{providerTrackId}",
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