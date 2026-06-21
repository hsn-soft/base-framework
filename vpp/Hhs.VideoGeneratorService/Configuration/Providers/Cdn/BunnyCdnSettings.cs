using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class BunnyCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    // Bunny Storage (internal) - specific settings
    public string? AccountId { get; set; }
    public string? StorageRegion { get; set; }

    // Override with concrete type for JSON deserialization
    public new DefaultStorageSettings Storage { get; set; } = default!;
}
