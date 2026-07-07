using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

/// <summary>
/// Bunny.net's S3-compatible storage backing (e.g. BackBlaze B2), fronted by a Bunny Pull Zone for
/// public serving. BaseUrl/ApiKey come from the inherited base: BaseUrl is the S3-compatible
/// endpoint (e.g. "https://s3.eu-central-003.backblazeb2.com"), ApiKey is the S3 Access Key ID.
/// </summary>
public sealed class CdnBunnyS3Settings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnBunnyS3";

    /// <summary>S3 Secret Access Key.</summary>
    public string ApiSecret { get; set; } = default!;

    /// <summary>The S3 bucket name, e.g. "assets-techsummus".</summary>
    public string BucketName { get; set; } = default!;

    /// <summary>The Pull Zone's public serving domain suffix, e.g. ".b-cdn.net".</summary>
    public string PullZoneUrl { get; set; } = default!;

    /// <summary>Optional key prefix within the bucket (no leading/trailing slash needed).</summary>
    public string Path { get; set; } = "";
}