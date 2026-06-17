using Hhs.Shared.Providers;

namespace Hhs.VideoGeneratorService.Providers;

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

public sealed class VideoCreateRequest
{
    public string VideoInputJson { get; set; } = default!;
    public List<string> AudioUrls { get; set; } = [];
    public List<string> AudioFilePaths { get; set; } = [];
}

public sealed class VideoCreateResponse
{
    public bool IsCompleted { get; set; }
    public string? ProviderTrackId { get; set; }
    public string? ProviderFileUrl { get; set; }
    public string? FileName { get; set; }
}

public sealed class VideoStatusResponse
{
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? ProviderFileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }
}

public interface IVideoProvider
{
    string ProviderKey { get; }
    VideoProviderCapabilities Capabilities { get; }

    Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request,
        CancellationToken cancellationToken);

    Task<VideoStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken);
}