namespace Hhs.Shared.Configuration;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public string? DistributionId { get; set; }
}
