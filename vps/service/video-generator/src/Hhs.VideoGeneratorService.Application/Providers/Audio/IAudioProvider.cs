using Hhs.Shared.Helper.Providers;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

public interface IAudioProvider
{
    string ProviderKey { get; }
    AudioProviderCapabilities Capabilities { get; }

    Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request);

    Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId);
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