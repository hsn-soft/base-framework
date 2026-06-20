using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class LocalMinioCdnSettings : CdnProviderSettingsBase
{
    // LocalMinio-specific settings
    public string? MinioAccessKey { get; set; }
    public string? MinioSecretKey { get; set; }
    public string? MinioBucket { get; set; }
}
