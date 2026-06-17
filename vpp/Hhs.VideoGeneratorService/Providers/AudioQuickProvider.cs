using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class AudioQuickProvider : IAudioProvider
{
    private readonly HttpClient _httpClient;

    public AudioQuickProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => "audio-quick";

    public AudioProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public async Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5042/audio/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var fileUrl = json.GetProperty("remoteFileUrl").GetString();
        var fileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new AudioCreateResponse
        {
            IsCompleted = true,
            ProviderFileUrl = fileUrl,
            FileName = fileName
        };
    }

    public async Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5042/audio/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new AudioStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = json.GetProperty("remoteFileUrl").GetString(),
            FileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null
        };
    }
}