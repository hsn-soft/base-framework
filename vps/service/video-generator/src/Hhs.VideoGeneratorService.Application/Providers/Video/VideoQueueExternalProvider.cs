using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Domain.Constants;

namespace Hhs.VideoGeneratorService.Application.Providers.Video;

public sealed class VideoQueueExternalProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public VideoQueueExternalProvider(HttpClient httpClient, VideoQueueExternalProviderSettings videoSettings)
    {
        _httpClient = httpClient;
        _baseUrl = videoSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.VideoQueueExternal;

    public VideoProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling, AudioInputMode = VideoAudioInputMode.AudioUrlListRequired };

    public async Task<VideoCreateResponse> CreateAsync(VideoCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/video/generate", request);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? trackingId = json.GetProperty("trackingId").GetString();

        return new VideoCreateResponse { IsCompleted = false, ProviderTrackId = trackingId };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/video/status/{providerTrackId}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? status = json.GetProperty("status").GetString();

        if (status != ProviderStatusConstants.Completed)
        {
            if (status != ProviderStatusConstants.Failed)
            {
                return new VideoStatusResponse { IsProcessed = false };
            }

            string? error = json.GetProperty("error").GetString();
            return new VideoStatusResponse { IsProcessed = false, IsFailed = true, ErrorMessage = error };
        }

        string? fileUrl = status == ProviderStatusConstants.Completed ? json.GetProperty("remoteFileUrl").GetString() : null;
        string? fileName = status == ProviderStatusConstants.Completed && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new VideoStatusResponse { IsProcessed = true, IsFailed = false, ProviderFileUrl = fileUrl, FileName = fileName };
    }
}