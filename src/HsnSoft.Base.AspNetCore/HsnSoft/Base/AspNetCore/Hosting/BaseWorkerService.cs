using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Hosting;

public abstract class BaseWorkerService<TService> : BackgroundService
{
    private readonly IBaseLogger _logger;
    private readonly List<Task> _workers;
    private readonly int _workerCount;
    private readonly int _waitPeriodSeconds;
    private readonly int _maxWaitPeriodSecondsForTerminating = 30;

    protected BaseWorkerService(IBaseLogger logger, int workerCount = 10, int waitPeriodSeconds = 1)
    {
        _logger = logger;
        _workerCount = workerCount < 1 ? 1 : workerCount;
        _waitPeriodSeconds = waitPeriodSeconds < 1 ? 1 : waitPeriodSeconds;

        _workers = new List<Task>();
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        _logger.LogInformation("{Worker} | STARTING...", typeof(TService).Name);


        var loopCount = 0;
        while (!stopToken.IsCancellationRequested)
        {
            _logger.LogInformation("{Worker} | WORKER COUNT IS {WorkerCount}", typeof(TService).Name, _workerCount);

            for (var i = 1; i <= _workerCount; i++)
            {
                _workers.Add(new Task(
                    action: o =>
                    {
                        var processId = (o as ProcessModel)?.ProcessId ?? 0;

                        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | STARTED", typeof(TService).Name, processId);
                        var stopWatch = Stopwatch.StartNew();

                        // SOME OPERATION
                        SyncOperation(processId); // WAIT OPERATION COMPLETED

                        stopWatch.Stop();
                        var timespan = stopWatch.Elapsed;
                        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | FINISHED ({ProcessTime})sn", typeof(TService).Name, processId, timespan.TotalSeconds.ToString("0.###"));
                    },
                    state: new ProcessModel { ProcessId = i + loopCount * _workerCount },
                    cancellationToken: stopToken)
                );
            }

            Parallel.For(0, _workerCount, i =>
            {
                _workers[i].Start();
            });

            await Task.WhenAll(_workers).ContinueWith(_ =>
            {
                _workers.RemoveAll(t => t.IsCompleted);
            }, stopToken);

            _logger.LogDebug("{Worker} | ALL WORKER IS AVAILABLE", typeof(TService).Name);
            // loop wait period
            await Task.Delay(1000 * _waitPeriodSeconds, stopToken);
            loopCount++;
        }
    }

    protected abstract void SyncOperation(int workerId);

    public override void Dispose()
    {
        _logger.LogInformation("{Worker} | STOPPING...", typeof(TService).Name);

        var waitCounter = 0;
        _workers.RemoveAll(x => x.IsCompleted);
        while (_workers.Count > 0 && waitCounter < _maxWaitPeriodSecondsForTerminating)
        {
            _logger.LogInformation("{Worker} | Wait Background Worker Count [ {Count} ]", typeof(TService).Name, _workers.Count);

            Thread.Sleep(1000);
            _workers.RemoveAll(x => x.IsCompleted);
            waitCounter++;
        }

        base.Dispose();
        _logger.LogInformation("{Worker} | STOPPED", typeof(TService).Name);
    }
}