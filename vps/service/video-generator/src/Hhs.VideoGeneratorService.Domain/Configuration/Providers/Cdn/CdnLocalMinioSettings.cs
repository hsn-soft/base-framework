using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

public sealed class CdnLocalMinioSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnLocalMinio";
}
