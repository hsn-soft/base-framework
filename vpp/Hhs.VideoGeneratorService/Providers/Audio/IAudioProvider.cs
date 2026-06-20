using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers.Audio;

public interface IAudioProvider
{
    string ProviderKey { get; }
    AudioProviderCapabilities Capabilities { get; }

    Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request,
        CancellationToken cancellationToken);

    Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken);
}

public sealed class AudioCreateRequest
{
    public string InputText { get; set; } = default!;
}

public sealed class AudioCreateResponse
{
    public bool IsCompleted { get; set; }
    public string? ProviderTrackId { get; set; }
    public string? ProviderFileUrl { get; set; }
    public string? FileName { get; set; }
}

public sealed class AudioStatusResponse
{
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? ProviderFileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }
}