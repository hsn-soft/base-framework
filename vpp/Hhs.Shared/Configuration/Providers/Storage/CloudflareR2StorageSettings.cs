namespace Hhs.Shared.Configuration.Providers.Storage;

public sealed class CloudflareR2StorageSettings : StorageProviderSettingsBase
{
    public CloudflareR2StorageSettings()
    {
        Type = "CloudflareR2";
    }

    /// <summary>
    /// Cloudflare Account ID for R2 API
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Cloudflare R2 Access Key ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Cloudflare R2 Secret Access Key
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Cloudflare R2 bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Cloudflare R2 endpoint (e.g., https://accountid.r2.cloudflarestorage.com)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}
