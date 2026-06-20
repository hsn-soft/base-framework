namespace Hhs.Shared.Configuration.Providers;

/// <summary>
/// Base configuration for CDN providers.
/// Default values for ZonePath and PathPrefix are suitable for development/staging.
/// These should be customized per environment via appsettings.json.
/// </summary>
public class CdnProviderSettingsBase : IHasCdnBaseUrl
{
    /// <summary>
    /// Base URL for the CDN provider. Required.
    /// Example: "https://cdn.example.com", "https://bunnycdn.com"
    /// </summary>
    public string BaseUrl { get; set; } = default!;

    /// <summary>
    /// API key for CDN authentication. Optional.
    /// Required for providers that use key-based authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API secret for CDN authentication. Optional.
    /// Required for some providers that require both key and secret.
    /// </summary>
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Storage zone/path on the CDN. Default: "media"
    /// Controls which zone/directory files are uploaded to.
    /// Common values: "media", "videos", "assets", "content"
    /// </summary>
    public string ZonePath { get; set; } = "media";

    /// <summary>
    /// Path prefix for organized file storage. Default: "prod"
    /// Helps organize files by environment or category.
    /// Common values: "dev", "staging", "prod", "archive"
    /// </summary>
    public string PathPrefix { get; set; } = "prod";

    /// <summary>
    /// Provider-specific storage configuration. Optional.
    /// Used to store provider-specific settings.
    /// </summary>
    public object? Storage { get; set; }
}

/// <summary>
/// Marker interface for CDN settings that have BaseUrl, ZonePath, and PathPrefix properties.
/// </summary>
public interface IHasCdnBaseUrl
{
    string BaseUrl { get; }
    string ZonePath { get; }
    string PathPrefix { get; }
}
