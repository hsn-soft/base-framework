using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class AzureCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnAzure";

    public string? ProfileName { get; set; }

    public new StorageSettings Storage { get; set; } = default!;
}
