using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.Configuration;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineQueueProvider(HttpClient httpClient, OutlineQueueProviderSettings outlineSettings) : IOutlineProvider
{
    private readonly string _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;

    public string ProviderKey => ProviderKeys.OutlineQueue;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling };

    public async Task<OutlineCreateResponse> CreateAsync(OutlineCreateRequest request, CancellationToken cancellationToken)
    {
        var inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(inputText))
            inputText = "Default outline content";

        var mockRequest = new { InputText = inputText };
        var response = await httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
            mockRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        string? trackingId = json.GetProperty("trackingId").GetString();

        return new OutlineCreateResponse { IsProcessed = false, ProviderTrackId = trackingId };
    }

    public async Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(
            $"{_baseUrl}/outline/status/{request.ProviderTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        string? status = json.GetProperty("status").GetString();

        if (status != "completed")
        {
            return new OutlineStatusResponse { IsProcessed = false };
        }

        string? script = json.GetProperty("script").GetString();
        return new OutlineStatusResponse { IsProcessed = true, OutlinedData = script };
    }
}