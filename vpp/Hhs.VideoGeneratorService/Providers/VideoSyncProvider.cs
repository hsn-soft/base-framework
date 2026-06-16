using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoSyncProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;

    public VideoSyncProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => "video-sync";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult,
        AudioInputMode = VideoAudioInputMode.AudioFileRequired
    };

    public async Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AudioFilePaths.Count == 0)
            throw new InvalidOperationException("VideoSyncProvider requires audio file paths.");

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5045/video/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var fileUrl = json.GetProperty("remoteFileUrl").GetString();

        return new VideoCreateResponse
        {
            IsCompleted = true,
            ProviderFileUrl = fileUrl
        };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5045/video/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new VideoStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = json.GetProperty("remoteFileUrl").GetString()
        };
    }
}
