using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoProviderC : IVideoProvider
{
    public string ProviderKey => "video-c";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling,
        AudioInputMode = VideoAudioInputMode.AudioFileRequired
    };

    public Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AudioFilePaths.Count == 0)
            throw new InvalidOperationException("VideoProviderC requires audio file paths.");

        return Task.FromResult(new VideoCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = Guid.NewGuid().ToString("N")
        });
    }

    public Task<VideoStatusResponse> GetStatusAsync(string providerTrackId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new VideoStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = $"https://video-c/{providerTrackId}.mp4"
        });
    }
}