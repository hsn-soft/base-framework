using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CloudflareCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnCloudflare";

    // Cloudflare-specific settings
    public string? ZoneId { get; set; }
    public string? AccountId { get; set; }
    public string? NamespaceId { get; set; }

    public new DefaultStorageSettings Storage { get; set; } = default!;
}
