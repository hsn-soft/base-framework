using Hhs.Shared.Configuration;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class VideoRetrySettings : RetrySettingsBase
{
    public const string SectionName = "Retry:Video";

    public int RetryWorkerIntervalSeconds { get; set; } = 10;
}
