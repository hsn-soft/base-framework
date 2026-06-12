namespace Hhs.VideoGeneratorService.Domain.Settings;

public sealed class VideoRequestQuerySettings
{
    // public bool IsEnabledVideoRequestQuery { get; set; } = true;
    // public int WorkerRunPeriodForSeconds { get; set; } = 60;
    public int ReQueryWaitPeriodForSeconds { get; set; } = 300;
    public int ReQueryLimit { get; set; } = 10;
    public int QueryPackageCount { get; set; } = 10;
}