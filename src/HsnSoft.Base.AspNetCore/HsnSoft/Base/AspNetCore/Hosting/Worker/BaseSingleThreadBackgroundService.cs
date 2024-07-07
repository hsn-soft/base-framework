using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Hosting.Worker;

public abstract class BaseSingleThreadBackgroundService<TService> : BackgroundService, IBaseThreadBackgroundService
    where TService : IBaseThreadBackgroundService
{
    protected readonly IFrameworkLogger Logger;

    private bool WaitContinuousThread { get; }

    private int WaitPeriodSeconds { get; }

    private bool IsProcessing { get; set; }

    private bool SkipWaitPeriod { get; set; }

    protected BaseSingleThreadBackgroundService(IFrameworkLogger logger, int waitPeriodSeconds = 1, bool waitContinuousThread = false)
    {
        Logger = logger;
        WaitPeriodSeconds = waitPeriodSeconds < 1 ? 1 : waitPeriodSeconds;
        WaitContinuousThread = waitContinuousThread;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogDebug("{Worker} | STARTED", typeof(TService).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            IsProcessing = true;
            try
            {
                Logger.LogDebug("{Worker} | BEGIN", typeof(TService).Name);
                var stopWatch = Stopwatch.StartNew();

                OperationAsync(stoppingToken).GetAwaiter().GetResult(); // WAIT OPERATION COMPLETED

                stopWatch.Stop();
                var timespan = stopWatch.Elapsed;
                Logger.LogDebug("{Worker} | END ({ProcessTime})sn", typeof(TService).Name, timespan.TotalSeconds.ToString("0.###"));
            }
            catch (Exception ex)
            {
                Logger.LogError("{Worker} | FAILED: {ExMessage}", typeof(TService).Name, ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }

            for (var i = 0; i < WaitPeriodSeconds; i++)
            {
                await Task.Delay(1000, stoppingToken);
                if (!SkipWaitPeriod) continue;
                SkipWaitPeriod = false;
                break;
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("{Worker} | STOPPING...", typeof(TService).Name);
        while (IsProcessing && WaitContinuousThread)
        {
            // Wait is uncompleted tasks
            Thread.Sleep(500);
        }

        Logger.LogDebug("{Worker} | STOPPED", typeof(TService).Name);
        // Send cancellation token
        return base.StopAsync(cancellationToken);
    }

    public abstract Task OperationAsync(CancellationToken cancellationToken);

    public void SkipOperationWaitPeriod() => SkipWaitPeriod = true;
}