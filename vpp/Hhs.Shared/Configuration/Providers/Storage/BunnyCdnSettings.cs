namespace Hhs.Shared.Configuration.Providers.Storage;

public sealed class BunnyCdnSettings : CdnProviderSettingsBase
{
    // Bunny-specific settings
    public string? AccountId { get; set; }
    public string? StorageRegion { get; set; }
    public string? AccessKey { get; set; }
}
