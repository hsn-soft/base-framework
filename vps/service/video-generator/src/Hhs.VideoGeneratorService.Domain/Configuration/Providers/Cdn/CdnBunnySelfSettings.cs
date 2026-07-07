using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

/// <summary>
/// Bunny.net Edge Storage (real Storage Zone, not the S3-compatible one — see CdnBunnyS3Settings
/// for that). BaseUrl/ApiKey come from the inherited base: BaseUrl is the storage API endpoint
/// (e.g. "https://storage.bunnycdn.com"), ApiKey is the Storage Zone's "AccessKey".
/// </summary>
public sealed class CdnBunnySelfSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnySelf";

    /// <summary>The Bunny Storage Zone name, e.g. "techsummus".</summary>
    public string StorageZoneName { get; set; } = default!;

    /// <summary>The Pull Zone's public serving domain suffix, e.g. ".b-cdn.net".</summary>
    public string PullZoneUrl { get; set; } = default!;

    /// <summary>Optional key prefix within the zone (no leading/trailing slash needed).</summary>
    public string Path { get; set; } = "";
}