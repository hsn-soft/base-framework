using Hhs.Shared.Helper.Configuration.Providers;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

public sealed class CdnBunnyS3Settings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnyS3";

    public string ZonePath { get; set; } = "media";

    public string PathPrefix { get; set; } = "prod";

    [CanBeNull] public string AccountId { get; set; }
    [CanBeNull] public string Region { get; set; }

    // S3 Storage configuration
    [CanBeNull] public string StorageType { get; set; }
    [CanBeNull] public string StorageEndpointUrl { get; set; }
    [CanBeNull] public string StorageApiKey { get; set; }
    [CanBeNull] public string StorageSecretKey { get; set; }
}