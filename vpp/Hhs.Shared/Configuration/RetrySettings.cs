namespace Hhs.Shared.Configuration;

public class RetrySettings
{
    public int[] DelaySeconds { get; set; } = [60, 120, 300, 900];
    public int MaxRetryCount { get; set; } = 4;
    public int RetryWorkerIntervalSeconds { get; set; } = 10;
    public int ClaimFailRescheduleDelaySeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
}
