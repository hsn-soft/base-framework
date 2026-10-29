using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Hosting;

/// <summary>
/// A background worker that does its job every <c>interval</c>: once right at start, then on a steady period. A failed run is
/// logged and the worker keeps going, so one bad run (a database that is briefly down) does not stop an agent for good.
/// Derive, implement <see cref="RunOnceAsync"/> and register with <c>AddHostedService</c>.
/// </summary>
public abstract partial class PeriodicBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;

    protected PeriodicBackgroundService(TimeSpan interval, ILogger logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(logger);

        _interval = interval;
        _logger = logger;
    }

    /// <summary>One unit of work. Throwing is allowed: the exception is logged and the next run still happens.</summary>
    protected abstract Task RunOnceAsync(CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogRunFailed(_logger, exception, GetType().Name);
            }

            try
            {
                // the timer is ours and lives until the loop ends, so this only ever yields "true" or throws on cancellation
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(EventId = 5001, EventName = "PERIODIC_SERVICE_RUN_FAILED", Level = LogLevel.Error, Message = "{Worker} run failed, trying again at the next tick")]
    private static partial void LogRunFailed(ILogger logger, Exception exception, string worker);
}
