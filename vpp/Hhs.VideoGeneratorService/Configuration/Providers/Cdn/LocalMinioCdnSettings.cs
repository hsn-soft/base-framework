using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class LocalMinioCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnLocalMinio";

    // Override with concrete type for JSON deserialization
    public new StorageSettings Storage { get; set; } = default!;
}
