namespace Hhs.VideoGeneratorService.Configuration;

public sealed class VideoEntityDefaults
{
    public const string SectionName = "EntityDefaults:Video";

    public int AudioMaxRetryCount { get; set; } = 30;
    public int AudioMaxProviderPollingCount { get; set; } = 60;
    public int VideoMaxRetryCount { get; set; } = 30;
    public int VideoMaxProviderPollingCount { get; set; } = 60;
}
