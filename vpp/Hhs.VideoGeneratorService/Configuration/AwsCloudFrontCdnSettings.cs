using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public string? DistributionId { get; set; }
}
