using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class VideoProviderA : IVideoProvider
{
    public string ProviderKey => "video-a";

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult,
        AudioInputMode = VideoAudioInputMode.ProviderCreatesAudio
    };

    public Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new VideoCreateResponse
        {
            IsCompleted = true,
            ProviderFileUrl = $"https://video-a/{Guid.NewGuid():N}.mp4"
        });
    }

    public Task<VideoStatusResponse> GetStatusAsync(string providerTrackId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}