using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoProProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;

    public VideoProProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => "video-pro";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling,
        AudioInputMode = VideoAudioInputMode.AudioFileRequired
    };

    public async Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AudioFilePaths.Count == 0)
            throw new InvalidOperationException("VideoProviderC requires audio file paths.");

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5047/video/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var trackingId = json.GetProperty("trackingId").GetString();

        return new VideoCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = trackingId
        };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5047/video/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var status = json.GetProperty("status").GetString();
        var fileUrl = status == "completed" ? json.GetProperty("remoteFileUrl").GetString() : null;
        var fileName = status == "completed" && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

        return new VideoStatusResponse
        {
            IsCompleted = status == "completed",
            ProviderFileUrl = fileUrl,
            FileName = fileName
        };
    }
}