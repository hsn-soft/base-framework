namespace Hhs.Shared.Configuration.Providers.Audio;

public abstract class AudioProviderSettings : ProviderSettingsBase
{
    public string AudioQuality { get; set; } = "high";
    public int MaxDurationSeconds { get; set; } = 3600;
    public long MaxFileSize { get; set; } = 52428800; // 50MB
}
