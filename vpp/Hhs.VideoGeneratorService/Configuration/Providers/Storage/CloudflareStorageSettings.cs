using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Storage;

/// <summary>
/// Cloudflare R2 or Cloudflare-compatible storage configuration.
/// Provides settings for external HTTP CDN storage like Cloudflare or similar services.
/// </summary>
public sealed class CloudflareStorageSettings : StorageProviderSettingsBase
{
    /// <summary>
    /// Cloudflare Zone ID or similar identifier for the storage zone.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Account identifier for the external storage service.
    /// Example: Cloudflare account ID, Bunny account ID, etc.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Namespace identifier for the storage bucket/container.
    /// </summary>
    public string? NamespaceId { get; set; }
}
