using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoProviderB : IVideoProvider
{
    public string ProviderKey => "video-b";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling,
        AudioInputMode = VideoAudioInputMode.AudioUrlListRequired
    };

    public Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AudioUrls.Count == 0)
            throw new InvalidOperationException("VideoProviderB requires audio urls.");

        return Task.FromResult(new VideoCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = Guid.NewGuid().ToString("N")
        });
    }

    public Task<VideoStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new VideoStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = $"https://video-b/{providerTrackId}.mp4"
        });
    }
}