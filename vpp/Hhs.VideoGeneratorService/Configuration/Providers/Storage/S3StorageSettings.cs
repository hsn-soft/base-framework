using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Storage;

/// <summary>
/// S3-specific storage configuration (AWS S3, MinIO, etc.)
/// </summary>
public sealed class S3StorageSettings : StorageProviderSettingsBase
{
    /// <summary>
    /// AWS region (e.g., "us-east-1", "eu-west-1"). Optional for S3-compatible services.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Force path-style addressing (required for MinIO and some S3-compatible services).
    /// Default: true for S3-compatible, false for AWS S3.
    /// </summary>
    public bool? UsePathStyle { get; set; }

    /// <summary>
    /// Custom S3 endpoint (overrides Url if specified). Used for S3-compatible services like MinIO.
    /// Example: "http://minio:9000"
    /// </summary>
    public string? EndpointUrl { get; set; }
}
