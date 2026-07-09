using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Domain.Constants;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

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

    public AudioProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling };

    public async Task<AudioCreateResponse> CreateAsync(AudioCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/audio/generate", request);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? trackingId = json.GetProperty("trackingId").GetString();

        return new AudioCreateResponse { IsCompleted = false, ProviderTrackId = trackingId };
    }

    public async Task<AudioStatusResponse> GetStatusAsync(string providerTrackId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/audio/status/{providerTrackId}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? status = json.GetProperty("status").GetString();

        if (status != ProviderStatusConstants.Completed)
        {
            if (status != ProviderStatusConstants.Failed)
            {
                return new AudioStatusResponse { IsProcessed = false };
            }

            string? error = json.GetProperty("error").GetString();
            return new AudioStatusResponse { IsProcessed = false, IsFailed = true, ErrorMessage = error };
        }

        string? fileUrl = status == ProviderStatusConstants.Completed ? json.GetProperty("remoteFileUrl").GetString() : null;
        string? fileName = status == ProviderStatusConstants.Completed && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new AudioStatusResponse { IsProcessed = true, IsFailed = false, ProviderFileUrl = fileUrl, FileName = fileName };
    }
}