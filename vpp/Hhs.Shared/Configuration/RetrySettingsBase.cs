namespace Hhs.Shared.Configuration;

public abstract class RetrySettingsBase
{
    public int[] DelaySeconds { get; set; } = [60, 120, 300, 900];
    public int MaxRetryCount { get; set; } = 4;
    public bool RetryOnTimeout { get; set; } = true;
    public bool RetryOnTransientError { get; set; } = true;
}
