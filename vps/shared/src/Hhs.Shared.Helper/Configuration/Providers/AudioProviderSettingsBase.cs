namespace Hhs.Shared.Helper.Configuration.Providers;

/// <summary>
/// Base configuration for audio providers.
/// Default values represent common requirements for audio generation.
/// These can be overridden per provider via appsettings.json.
/// </summary>
public abstract class AudioProviderSettingsBase : ProviderSettingsBase
{
    /// <summary>
    /// Audio quality setting. Default: "high"
    /// Common values: "high", "medium", "low"
    /// </summary>
    public string AudioQuality { get; set; } = "high";

    /// <summary>
    /// Maximum audio duration in seconds. Default: 3600 (1 hour)
    /// </summary>
    public int MaxDurationSeconds { get; set; } = 3600;

    /// <summary>
    /// Maximum file size in bytes. Default: 52428800 (50MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 52428800; // 50MB
}
