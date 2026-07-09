using JetBrains.Annotations;

namespace Hhs.Shared.Helper.Retry;

public sealed class RetryDelayCalculator([CanBeNull] int[] delaySeconds)
{
    private readonly int[] _delaySeconds = delaySeconds ?? [60, 120, 300, 900];

    public TimeSpan Calculate(int retryCount)
    {
        if (retryCount < 0) retryCount = 0;
        int index = Math.Min(retryCount, _delaySeconds.Length - 1);
        return TimeSpan.FromSeconds(_delaySeconds[index]);
    }

    public static TimeSpan CalculateDefault(int retryCount)
    {
        var calculator = new RetryDelayCalculator([60, 120, 300, 900]);
        return calculator.Calculate(retryCount);
    }
}