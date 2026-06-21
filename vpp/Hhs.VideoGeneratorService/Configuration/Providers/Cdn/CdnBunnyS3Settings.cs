using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnBunnyS3Settings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnyS3";

    // Bunny CDN with S3 backend - S3-specific settings
    public string? AccountId { get; set; }
    public string? StorageRegion { get; set; }

    // Override with concrete type for JSON deserialization
    public new S3StorageSettings Storage { get; set; } = default!;
}
