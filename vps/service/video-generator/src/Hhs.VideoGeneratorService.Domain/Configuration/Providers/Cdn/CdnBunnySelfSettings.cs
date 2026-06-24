using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

public sealed class CdnBunnySelfSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    public string ZonePath { get; set; } = "media";

    public string PathPrefix { get; set; } = "prod";

    public string? AccountId { get; set; }
    public string? Region { get; set; }
}