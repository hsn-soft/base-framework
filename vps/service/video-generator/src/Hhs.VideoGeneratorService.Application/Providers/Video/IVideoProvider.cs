using Hhs.Shared.Helper.Providers;

namespace Hhs.VideoGeneratorService.Application.Providers.Video;

public interface IVideoProvider
{
    string ProviderKey { get; }
    VideoProviderCapabilities Capabilities { get; }

    Task<VideoCreateResponse> CreateAsync(
        VideoCreateRequest request);

    Task<VideoStatusResponse> GetStatusAsync(
        string providerTrackId);
}

public sealed class VideoCreateRequest
{
    public string VideoInputJson { get; set; } = default!;
    public List<string> AudioUrls { get; set; } = [];
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