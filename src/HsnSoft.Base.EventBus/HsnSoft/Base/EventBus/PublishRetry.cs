using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus;

/// <summary>Repeats an operation that fails because the broker is not reachable (yet): exponential pause, a limited number of retries.</summary>
public static class PublishRetry
{
    /// <summary>
    /// Runs <paramref name="action"/>; when it throws an exception <paramref name="isTransient"/> accepts, waits <c>2^n * baseDelay</c>
    /// and tries again, up to <paramref name="retryCount"/> times. The last failure, and any other exception, is thrown.
    /// </summary>
    /// <param name="action">What to do.</param>
    /// <param name="retryCount">How often to repeat after the first try.</param>
    /// <param name="baseDelay">The unit of the pause.</param>
    /// <param name="isTransient">Tells whether a failure is worth another try.</param>
    /// <param name="onRetry">Called before every pause with the failure, the pause and the number of the retry.</param>
    /// <param name="cancellationToken">Cancels the waiting.</param>
    public static async Task ExecuteAsync(
        Func<Task> action,
        int retryCount,
        TimeSpan baseDelay,
        Func<Exception, bool> isTransient,
        Action<Exception, TimeSpan, int> onRetry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(isTransient);
        ArgumentNullException.ThrowIfNull(onRetry);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception exception) when (attempt <= retryCount && exception is not OperationCanceledException && isTransient(exception))
            {
                TimeSpan delay = baseDelay * Math.Pow(2, attempt);
                onRetry(exception, delay, attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
