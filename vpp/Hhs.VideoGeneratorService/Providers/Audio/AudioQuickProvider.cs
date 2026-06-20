using System.Text.Json;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Audio;

namespace Hhs.VideoGeneratorService.Providers.Audio;

public sealed class AudioQuickProvider : IAudioProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public AudioQuickProvider(HttpClient httpClient, AudioFastProviderSettings audioSettings)
    {
        _httpClient = httpClient;
        _baseUrl = audioSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.AudioQuick;

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
            $"{_baseUrl}/audio/generate",
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
            $"{_baseUrl}/audio/status/{providerTrackId}",
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
