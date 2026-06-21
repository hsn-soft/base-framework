using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnAbcCloudFrontSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnAbcCloudFront";

    public string? ZoneId { get; set; }
    public string? AccountId { get; set; }
    public string? NamespaceId { get; set; }

    public string? StorageType { get; set; }
    public string? StorageEndpointUrl { get; set; }
    public string? StorageApiKey { get; set; }
    public string? StorageSecretKey { get; set; }
}