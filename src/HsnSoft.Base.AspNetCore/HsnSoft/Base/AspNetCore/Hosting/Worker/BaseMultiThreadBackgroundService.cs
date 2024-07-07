using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Hosting.Worker;

public abstract class BaseMultiThreadBackgroundService<TService> : BackgroundService, IBaseThreadBackgroundService
    where TService : IBaseThreadBackgroundService
{
    protected ushort MultiThreadCount { get; }
    protected IFrameworkLogger Logger { get; }

    private bool WaitContinuousThread { get; }

    private int WaitPeriodSeconds { get; }

    private bool SkipWaitPeriod { get; set; }

    private readonly List<Task> _workers = new();
    private const int MaxWaitPeriodSecondsForTerminating = 30;

    protected BaseMultiThreadBackgroundService(IFrameworkLogger logger, ushort multiThreadCount = 1, int waitPeriodSeconds = 1, bool waitContinuousThread = false)
    {
        Logger = logger;
        MultiThreadCount = (ushort)(multiThreadCount < 1 ? 1 : multiThreadCount);
        WaitPeriodSeconds = waitPeriodSeconds < 1 ? 1 : waitPeriodSeconds;
        WaitContinuousThread = waitContinuousThread;
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        Logger.LogDebug("{Worker} | STARTED | WORKER COUNT [{WorkerCount}]", typeof(TService).Name, MultiThreadCount);

        while (!stopToken.IsCancellationRequested)
        {
            for (var i = 1; i <= MultiThreadCount; i++)
            {
                _workers.Add(new Task(
                    action: o =>
                    {
                        var processId = (o as ProcessModel)?.ProcessId ?? 0;
                        try
                        {
                            Logger.LogDebug("{Worker} | WORKER[{WorkerId}] | BEGIN", typeof(TService).Name, processId);
                            var stopWatch = Stopwatch.StartNew();

                            OperationAsync(stopToken).GetAwaiter().GetResult(); // WAIT OPERATION COMPLETED

                            stopWatch.Stop();
                            var timespan = stopWatch.Elapsed;
                            Logger.LogDebug("{Worker} | WORKER[{WorkerId}] | END ({ProcessTime})sn", typeof(TService).Name, processId, timespan.TotalSeconds.ToString("0.###"));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ExMessage}", typeof(TService).Name, processId, ex.Message);
                        }
                    },
                    state: new ProcessModel { ProcessId = i },
                    cancellationToken: stopToken)
                );
            }

            Parallel.For(0, MultiThreadCount, i =>
            {
                _workers[i].Start();
            });

            await Task.WhenAll(_workers).ContinueWith(_ =>
            {
                _workers.RemoveAll(t => t.IsCompleted);
            }, stopToken);

            Logger.LogDebug("{Worker} | ALL WORKER IS AVAILABLE", typeof(TService).Name);

            for (var i = 0; i < WaitPeriodSeconds; i++)
            {
                await Task.Delay(1000, stopToken);
                if (!SkipWaitPeriod) continue;
                SkipWaitPeriod = false;
                break;
            }
        }
    }

    public abstract Task OperationAsync(CancellationToken cancellationToken);

    public void SkipOperationWaitPeriod() => SkipWaitPeriod = true;

    public override void Dispose()
    {
        Logger.LogDebug("{Worker} | TERMINATING...", typeof(TService).Name);

        var waitCounter = 0;
        _workers.RemoveAll(x => x.IsCompleted);
        while (WaitContinuousThread && _workers.Count > 0 && waitCounter < MaxWaitPeriodSecondsForTerminating)
        {
            Logger.LogDebug("{Worker} | Wait Background Worker Count [ {Count} ]", typeof(TService).Name, _workers.Count);

            Thread.Sleep(1000);
            _workers.RemoveAll(x => x.IsCompleted);
            waitCounter++;
        }

        base.Dispose();
        Logger.LogDebug("{Worker} | TERMINATED", typeof(TService).Name);
    }
}