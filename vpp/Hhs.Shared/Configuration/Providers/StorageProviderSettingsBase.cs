namespace Hhs.Shared.Configuration.Providers;

/// <summary>
/// Base configuration for storage providers (S3, Azure, etc.).
/// All properties should be configured via appsettings.json or environment variables.
/// Required fields: Type, Url, ApiKey
/// </summary>
public abstract class StorageProviderSettingsBase
{
    /// <summary>
    /// Storage provider type. Required.
    /// Examples: "s3", "azure-blob", "minio", "gcs"
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Storage service endpoint URL. Required.
    /// Examples: "https://s3.amazonaws.com", "https://myaccount.blob.core.windows.net"
    /// </summary>
    public string Url { get; set; } = default!;

    /// <summary>
    /// API key or access key for authentication. Required.
    /// For AWS S3: Access Key ID
    /// For Azure: Storage Account Name or Key
    /// </summary>
    public string ApiKey { get; set; } = default!;

    /// <summary>
    /// Secret key or password for authentication. Optional.
    /// For AWS S3: Secret Access Key
    /// For Azure: Often included in connection string
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Bucket name or container name. Optional.
    /// Specifies where files should be stored within the service.
    /// </summary>
    public string? BucketOrContainer { get; set; }
}
