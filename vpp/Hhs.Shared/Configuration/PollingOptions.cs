namespace Hhs.Shared.Configuration;

public sealed class PollingOptions
{
    public const string SectionName = "Polling";

    public int OutlinePollingIntervalSeconds { get; set; } = 5;
    public int AudioPollingIntervalSeconds { get; set; } = 5;
    public int VideoPollingIntervalSeconds { get; set; } = 5;

    public int MaxOutlinePollingAttempts { get; set; } = 10;
    public int MaxAudioPollingAttempts { get; set; } = 10;
    public int MaxVideoPollingAttempts { get; set; } = 10;
}

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    public int[] DelaySeconds { get; set; } = [60, 120, 300, 900];
    public int MaxRetryCount { get; set; } = 4;
}

public sealed class TimeoutOptions
{
    public const string SectionName = "Timeout";

    public int OutlineProviderTimeoutSeconds { get; set; } = 30;
    public int AudioProviderTimeoutSeconds { get; set; } = 30;
    public int VideoProviderTimeoutSeconds { get; set; } = 60;
}
