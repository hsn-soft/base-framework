namespace Hhs.Shared.Helper.Configuration;

public abstract class RetrySettingsBase
{
    public int[] DelaySeconds { get; set; } = [60, 120, 300, 900];
    public int MaxRetryCount { get; set; } = 4;
    public int RetryWorkerIntervalSeconds { get; set; } = 10;
    public int ClaimFailRescheduleDelaySeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// How long (in minutes) an EventInboxMessage may stay in 'Started' status before being
    /// considered stale and reset to 'Failed' so the next broker re-delivery can process it.
    /// Default: 15 minutes.
    /// </summary>
    public int StaleInboxMessageThresholdMinutes { get; set; } = 15;
}
