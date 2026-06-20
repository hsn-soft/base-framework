namespace Hhs.Shared.Configuration;

public sealed class CloudflareCdnSettings : CdnProviderSettingsBase
{
    // Cloudflare-specific settings
    public string? ZoneId { get; set; }
    public string? AccountId { get; set; }
    public string? NamespaceId { get; set; }
}
