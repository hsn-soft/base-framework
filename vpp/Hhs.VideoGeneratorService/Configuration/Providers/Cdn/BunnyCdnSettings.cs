using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class BunnyCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    // Bunny-specific settings
    public string? AccountId { get; set; }
    public string? StorageRegion { get; set; }
    public string? AccessKey { get; set; }
}
