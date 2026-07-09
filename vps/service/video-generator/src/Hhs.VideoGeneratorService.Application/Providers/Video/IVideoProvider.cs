using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using JetBrains.Annotations;

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
    public List<string> AudioCdnUrls { get; set; } = [];
    public ContentType RefContentType { get; set; }

    /// <summary>
    /// Opaque per-customer provider configuration (CustomerVpSetting.VideoGenerationProviderSettings),
    /// e.g. ClientCreatomateSettings — template ids, branding, and video dimensions genuinely vary
    /// per customer, unlike a provider's own API key/base settings which come from appsettings.
    /// Providers that don't need per-customer configuration (VideoQueueExternal/Internal) ignore it.
    /// </summary>
    [CanBeNull] public object CustomerProviderSettings { get; set; }
}

public sealed class VideoCreateResponse
{
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }

    /// <summary>Only meaningful when IsFailed is true — set by the provider's own error classification.</summary>
    public bool IsRetryable { get; set; }

    [CanBeNull] public string ErrorMessage { get; set; }
    [CanBeNull] public string ProviderTrackId { get; set; }
    [CanBeNull] public string ProviderFileUrl { get; set; }
    [CanBeNull] public string FileName { get; set; }
}

public sealed class VideoStatusResponse
{
    public bool IsProcessed { get; set; }
    public bool IsFailed { get; set; }

    /// <summary>Only meaningful when IsFailed is true — set by the provider's own error classification.</summary>
    public bool IsRetryable { get; set; }

    [CanBeNull] public string ProviderFileUrl { get; set; }
    [CanBeNull] public string ErrorMessage { get; set; }
    [CanBeNull] public string FileName { get; set; }
}