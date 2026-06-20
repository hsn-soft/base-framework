using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AzureCdnSettings : CdnProviderSettingsBase
{
    public string? ProfileName { get; set; }
}
