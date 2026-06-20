using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class AudioHQProvider : IAudioProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public AudioHQProvider(HttpClient httpClient, AudioQueueProviderSettings audioSettings)
    {
        _httpClient = httpClient;
        _baseUrl = audioSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.AudioHQ;

    public AudioProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public async Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/audio/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var trackingId = json.GetProperty("trackingId").GetString();

        return new AudioCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = trackingId
        };
    }

    public async Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/audio/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var status = json.GetProperty("status").GetString();
        var fileUrl = status == "completed" ? json.GetProperty("remoteFileUrl").GetString() : null;
        var fileName = status == "completed" && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new AudioStatusResponse
        {
            IsCompleted = status == "completed",
            ProviderFileUrl = fileUrl,
            FileName = fileName
        };
    }
}
