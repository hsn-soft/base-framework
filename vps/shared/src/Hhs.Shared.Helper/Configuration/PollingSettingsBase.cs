namespace Hhs.Shared.Helper.Configuration;

public abstract class PollingSettings
{
    public int IntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public int BackoffIntervalSeconds { get; set; } = 5;
    public int ErrorRescheduleDelaySeconds { get; set; } = 5;
}
