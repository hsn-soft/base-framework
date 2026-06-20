namespace Hhs.Shared.Configuration.Providers.Video;

public abstract class VideoProviderSettingsBase : ProviderSettingsBase
{
    public bool AllowHorizontal { get; set; } = true;
    public bool AllowVertical { get; set; } = true;
    public string Resolution { get; set; } = "1080p";
    public string MediaFormat { get; set; } = "mp4";
}
