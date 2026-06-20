namespace Hhs.Shared.Configuration.Providers;

public abstract class AudioProviderSettingsBase : ProviderSettingsBase
{
    public string AudioQuality { get; set; } = "high";
    public int MaxDurationSeconds { get; set; } = 3600;
    public long MaxFileSize { get; set; } = 52428800; // 50MB
}
