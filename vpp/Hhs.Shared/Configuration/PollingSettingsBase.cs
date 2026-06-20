namespace Hhs.Shared.Configuration;

public abstract class PollingSettingsBase
{
    public int IntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public int BackoffIntervalSeconds { get; set; } = 5;
}
