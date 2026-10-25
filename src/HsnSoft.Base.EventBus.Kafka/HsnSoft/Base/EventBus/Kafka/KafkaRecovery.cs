using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>What keeps a consumer alive: when its own work fails (not a handler, which has its dead-letter topic) it is told, waits and starts again, until the service stops.</summary>
internal static class KafkaRecovery
{
    public static async Task RunAsync(Func<CancellationToken, Task> run, TimeSpan delay, Action<Exception> onError, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
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
                onError(exception);
                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
