using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>Rebuilds the persistent sink on a schedule (see <see cref="RefreshableSink"/>); the first run waits one interval.</summary>
internal sealed class GraylogSinkRefreshService(RefreshableSink sink, TimeSpan interval, ILogger<GraylogSinkRefreshService> logger) : PeriodicBackgroundService(interval, logger)
{
    private readonly RefreshableSink _sink = sink;
    private bool _first = true;

    protected override Task RunOnceAsync(CancellationToken cancellationToken)
    {
        // the sink was just built when the application started
        if (_first)
        {
            _first = false;
            return Task.CompletedTask;
        }

        _sink.Refresh();
        return Task.CompletedTask;
    }
}
