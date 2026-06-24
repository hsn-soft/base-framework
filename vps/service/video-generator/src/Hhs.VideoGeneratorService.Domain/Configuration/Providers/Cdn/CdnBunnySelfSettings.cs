using Hhs.Shared.Helper.Configuration.Providers;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

public sealed class CdnBunnySelfSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    public string ZonePath { get; set; } = "media";

    public string PathPrefix { get; set; } = "prod";

    [CanBeNull] public string AccountId { get; set; }
    [CanBeNull] public string Region { get; set; }
}