using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoFastExternalProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public VideoFastExternalProvider(HttpClient httpClient, VideoFastExternalProviderSettings videoSettings)
    {
        _httpClient = httpClient;
        _baseUrl = videoSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.VideoFastExternal;

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult,
        AudioInputMode = VideoAudioInputMode.AudioUrlListRequired
    };

    public async Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/video/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var fileUrl = json.GetProperty("remoteFileUrl").GetString();
        var fileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new VideoCreateResponse
        {
            IsCompleted = true,
            ProviderFileUrl = fileUrl,
            FileName = fileName
        };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/video/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new VideoStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = json.GetProperty("remoteFileUrl").GetString(),
            FileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null
        };
    }
}
