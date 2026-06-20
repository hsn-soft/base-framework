namespace Hhs.Shared.Configuration;

public abstract class VideoProviderSettings : ProviderSettingsBase
{
    public bool AllowHorizontal { get; set; } = true;
    public bool AllowVertical { get; set; } = true;
    public string Resolution { get; set; } = "1080p";
    public string MediaFormat { get; set; } = "mp4";
}
