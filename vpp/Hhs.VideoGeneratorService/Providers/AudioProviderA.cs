using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers;

public sealed class AudioProviderA : IAudioProvider
{
    public string ProviderKey => "audio-a";

    public AudioProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new AudioCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = Guid.NewGuid().ToString("N")
        });
    }

    public Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new AudioStatusResponse
        {
            IsCompleted = true,
            ProviderFileUrl = $"https://audio-a/{providerTrackId}.mp3"
        });
    }
}