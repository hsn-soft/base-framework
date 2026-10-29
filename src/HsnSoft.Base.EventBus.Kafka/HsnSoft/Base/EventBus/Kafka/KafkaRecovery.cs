using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>
/// What keeps a consumer alive: when its own work fails (not a handler, which has its dead-letter topic) it is told, waits and starts again, until the service stops. The wait doubles with every failure in a row, up to
/// <c>maxDelay</c>, so a broker that is down is not asked every few seconds by every consumer of every pod; a run that lasted longer than <c>maxDelay</c> was healthy, and the next failure starts from <c>delay</c> again.
/// </summary>
internal static class KafkaRecovery
{
    public static async Task RunAsync(Func<CancellationToken, Task> run, TimeSpan delay, TimeSpan maxDelay, Action<Exception, TimeSpan> onError, CancellationToken stoppingToken)
    {
        TimeSpan wait = delay;
        while (!stoppingToken.IsCancellationRequested)
        {
            long started = Stopwatch.GetTimestamp();
            try
            {
                await run(stoppingToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (Stopwatch.GetElapsedTime(started) > maxDelay)
                {
                    wait = delay;
                }

                onError(exception, wait);
                try
                {
                    await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                wait = TimeSpan.FromTicks(Math.Min(wait.Ticks * 2, Math.Max(maxDelay.Ticks, delay.Ticks)));
            }
        }
    }
}
