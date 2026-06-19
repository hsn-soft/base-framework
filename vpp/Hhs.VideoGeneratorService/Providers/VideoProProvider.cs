using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoProProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public VideoProProvider(HttpClient httpClient, IOptions<ProviderEndpointsOptions> options)
    {
        _httpClient = httpClient;
        _baseUrl = options.Value.VideoProviders.ProBaseUrl;
    }

    public string ProviderKey => ProviderKeys.VideoPro;

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
            $"{_baseUrl}/video/generate",
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
            $"{_baseUrl}/video/status/{providerTrackId}",
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