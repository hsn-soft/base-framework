namespace Hhs.Shared.Configuration.Providers;

/// <summary>
/// Base configuration for video providers.
/// Default values represent common requirements for video generation.
/// These can be overridden per provider via appsettings.json.
/// </summary>
public abstract class VideoProviderSettingsBase : ProviderSettingsBase
{
    /// <summary>
    /// Allow horizontal orientation videos. Default: true
    /// </summary>
    public bool AllowHorizontal { get; set; } = true;

    /// <summary>
    /// Allow vertical orientation videos. Default: true
    /// </summary>
    public bool AllowVertical { get; set; } = true;

    /// <summary>
    /// Video resolution. Default: "1080p"
    /// Common values: "720p", "1080p", "2k", "4k"
    /// </summary>
    public string Resolution { get; set; } = "1080p";

    /// <summary>
    /// Output video format/codec. Default: "mp4"
    /// Common values: "mp4", "mov", "webm", "avi"
    /// </summary>
    public string MediaFormat { get; set; } = "mp4";
}
