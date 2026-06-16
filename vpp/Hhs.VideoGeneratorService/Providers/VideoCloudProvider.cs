using Hhs.Shared.Providers;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoCloudProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;

    public VideoCloudProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => "video-cloud";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling,
        AudioInputMode = VideoAudioInputMode.AudioUrlListRequired
    };

    public async Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AudioUrls.Count == 0)
            throw new InvalidOperationException("VideoProviderB requires audio urls.");

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5046/video/generate",
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

    public async Task<VideoStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5046/video/status/{providerTrackId}",
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var status = json.GetProperty("status").GetString();
        var fileUrl = status == "completed" ? json.GetProperty("remoteFileUrl").GetString() : null;

        return new VideoStatusResponse
        {
            IsCompleted = status == "completed",
            ProviderFileUrl = fileUrl
        };
    }
}