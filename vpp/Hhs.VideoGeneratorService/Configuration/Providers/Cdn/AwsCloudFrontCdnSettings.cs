using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public string? DistributionId { get; set; }
}
