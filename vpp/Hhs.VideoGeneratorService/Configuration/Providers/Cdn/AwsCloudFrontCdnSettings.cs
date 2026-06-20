using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnAwsCloudFront";

    public string? DistributionId { get; set; }

    public new StorageSettings Storage { get; set; } = default!;
}
