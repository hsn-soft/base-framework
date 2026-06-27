using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineQueueProvider(HttpClient httpClient, OutlineQueueProviderSettings outlineSettings) : IOutlineProvider
{
    private readonly string _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;

    public string ProviderKey => ProviderKeys.OutlineQueue;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling };

    public async Task<OutlineCreateResponse> OutlineOperationAsync(OutlineCreateRequest request)
    {
        var inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(inputText))
            inputText = "Default outline content";

        var mockRequest = new { InputText = inputText };
        var response = await httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
            mockRequest
            );

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? trackingId = json.GetProperty("trackingId").GetString();

        return new OutlineCreateResponse { IsProcessed = false, ProviderTrackId = trackingId };
    }

    public async Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
    {
        var response = await httpClient.GetAsync(
            $"{_baseUrl}/outline/status/{request.ProviderTrackId}"
            );

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? status = json.GetProperty("status").GetString();

        if (status != "completed")
        {
            return new OutlineStatusResponse { IsProcessed = false };
        }

        string? script = json.GetProperty("script").GetString();
        return new OutlineStatusResponse { IsProcessed = true, OutlinedData = script };
    }
}