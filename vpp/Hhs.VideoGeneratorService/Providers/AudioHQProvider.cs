using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class AudioHQProvider : IAudioProvider
{
    private readonly HttpClient _httpClient;

    public AudioHQProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => "audio-hq";

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
            "http://localhost:5043/audio/generate",
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
            $"http://localhost:5043/audio/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var status = json.GetProperty("status").GetString();
        var fileUrl = status == "completed" ? json.GetProperty("remoteFileUrl").GetString() : null;

        return new AudioStatusResponse
        {
            IsCompleted = status == "completed",
            ProviderFileUrl = fileUrl
        };
    }
}
