using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnBunnyS3Settings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnyS3";

    // Bunny CDN with S3 backend - S3-specific settings
    public string? AccountId { get; set; }
    public string? StorageRegion { get; set; }
}
