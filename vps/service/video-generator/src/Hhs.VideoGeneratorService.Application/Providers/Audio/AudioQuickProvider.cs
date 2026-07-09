using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

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

    public AudioProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<AudioCreateResponse> CreateAsync(AudioCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/audio/generate", request);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? fileUrl = json.GetProperty("remoteFileUrl").GetString();
        string? fileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new AudioCreateResponse { IsCompleted = true, ProviderFileUrl = fileUrl, FileName = fileName };
    }

    public async Task<AudioStatusResponse> GetStatusAsync(string providerTrackId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/audio/status/{providerTrackId}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new AudioStatusResponse { IsProcessed = true, ProviderFileUrl = json.GetProperty("remoteFileUrl").GetString(), FileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null };
    }
}