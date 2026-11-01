using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// A sink that can be rebuilt while the application runs. A third-party sink (the Graylog HTTP one) can stay broken for the rest of the
/// process after a failed start; rebuilding it every few minutes bounds such an outage instead of needing a restart.
/// </summary>
/// <param name="factory">Builds the sink.</param>
/// <param name="retireDelay">
/// How long a replaced sink lives on before it is disposed (default 10 seconds): a log it was still sending when it was replaced
/// would be lost by an immediate dispose.
/// </param>
internal sealed class RefreshableSink(Func<ILogEventSink> factory, TimeSpan? retireDelay = null) : ILogEventSink, IDisposable
{
    private readonly Func<ILogEventSink> _factory = factory;
    private readonly TimeSpan _retireDelay = retireDelay ?? TimeSpan.FromSeconds(10);
    private readonly Lock _gate = new();
    private readonly List<IDisposable> _retiring = [];
    private volatile ILogEventSink _inner = factory();
    private bool _disposed;

    public void Emit(LogEvent logEvent) => _inner.Emit(logEvent);

    /// <summary>Builds a new sink, swaps it in and disposes the old one after the retire delay. When the new one cannot be built the old one stays.</summary>
    public void Refresh()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
        }

        ILogEventSink replacement;
        try
        {
            replacement = _factory();
        }
        catch (Exception exception)
        {
            SelfLog.WriteLine("The sink could not be rebuilt, the old one stays: {0}", exception);
            return;
        }

        ILogEventSink old = _inner;
        _inner = replacement;
        Retire(old);
    }

    private void Retire(ILogEventSink old)
    {
        if (old is not IDisposable disposable)
        {
            return;
        }

        if (_retireDelay <= TimeSpan.Zero)
        {
            disposable.Dispose();
            return;
        }

        lock (_gate)
        {
            // the application is stopping while the sink was swapped: nothing is left to wait for, and what the old one holds is sent now
            if (_disposed)
            {
                disposable.Dispose();
                return;
            }

            _retiring.Add(disposable);
        }

        _ = Task.Delay(_retireDelay).ContinueWith(_ => DisposeRetired(disposable), TaskScheduler.Default);
    }

    private void DisposeRetired(IDisposable disposable)
    {
        lock (_gate)
        {
            // already disposed by Dispose(), which does not wait for the delay
            if (!_retiring.Remove(disposable))
            {
                return;
            }
        }

        disposable.Dispose();
    }

    /// <summary>
    /// Disposes the current sink **and the ones that are still waiting out their delay**: at the end of the application a sink that was replaced a moment ago holds a batch that has not been sent (the Graylog sink sends in batches, when it is disposed
    /// it sends what is left), and the timer that would dispose it does not live to fire.
    /// </summary>
    public void Dispose()
    {
        IDisposable[] retiring;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            retiring = [.. _retiring];
            _retiring.Clear();
        }

        foreach (IDisposable disposable in retiring)
        {
            disposable.Dispose();
        }

        (_inner as IDisposable)?.Dispose();
    }
}
