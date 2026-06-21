using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnBunnySelfSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    public string? AccountId { get; set; }
    public string? Region { get; set; }
}