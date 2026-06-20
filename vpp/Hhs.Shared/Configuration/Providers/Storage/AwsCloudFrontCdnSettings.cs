namespace Hhs.Shared.Configuration.Providers.Storage;

public sealed class AwsCloudFrontCdnSettings : CdnProviderSettingsBase
{
    public string? DistributionId { get; set; }
}
