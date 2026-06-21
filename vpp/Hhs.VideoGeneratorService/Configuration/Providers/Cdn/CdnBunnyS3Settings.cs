using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnBunnyS3Settings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnyS3";

    public string? AccountId { get; set; }
    public string? Region { get; set; }

    public string? StorageType { get; set; }
    public string? StorageEndpointUrl { get; set; }
    public string? StorageApiKey { get; set; }
    public string? StorageSecretKey { get; set; }
}