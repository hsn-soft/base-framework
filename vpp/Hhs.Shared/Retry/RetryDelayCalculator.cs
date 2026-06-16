namespace Hhs.Shared.Retry;

public static class RetryDelayCalculator
{
    public static TimeSpan Calculate(int retryCount)
    {
        return retryCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(30)
        };
    }
}