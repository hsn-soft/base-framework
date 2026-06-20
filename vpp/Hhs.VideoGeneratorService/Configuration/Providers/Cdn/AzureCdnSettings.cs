using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class AzureCdnSettings : CdnProviderSettingsBase
{
    public string? ProfileName { get; set; }
}
