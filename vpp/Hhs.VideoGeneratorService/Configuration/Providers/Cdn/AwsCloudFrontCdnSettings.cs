using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnAwsCloudFront";

    public string? DistributionId { get; set; }
}
