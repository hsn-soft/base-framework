using HsnSoft.Base.Logging;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog.Persistent;

public sealed class FrameworkLogger : BaseLogger, IPersistentLogger
{
    public void PersistentInfoLog<T>(T t) where T : IPersistentLog => Write(LogEventLevel.Verbose, t);
    public void PersistentErrorLog<T>(T t) where T : IPersistentLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log) => Logger.Write(logLevel, "{@Log}", log);
}