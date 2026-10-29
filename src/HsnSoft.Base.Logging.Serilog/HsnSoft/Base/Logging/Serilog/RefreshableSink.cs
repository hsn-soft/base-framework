using System;
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
    private volatile ILogEventSink _inner = factory();

    public void Emit(LogEvent logEvent) => _inner.Emit(logEvent);

    /// <summary>Builds a new sink, swaps it in and disposes the old one after the retire delay. When the new one cannot be built the old one stays.</summary>
    public void Refresh()
    {
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

        _ = Task.Delay(_retireDelay).ContinueWith(_ => disposable.Dispose(), TaskScheduler.Default);
    }

    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
