using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class LocalMinioCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnLocalMinio";

    // LocalMinio-specific settings
    public string? MinioAccessKey { get; set; }
    public string? MinioSecretKey { get; set; }
    public string? MinioBucket { get; set; }
}
