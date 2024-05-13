using HsnSoft.Base.AspNetCore.Logging;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog.Persistent;

public sealed class RequestLogger : BaseLogger, IRequestResponseLogger
{
    public void RequestResponseInfoLog<T>(T t) where T : IRequestResponseLog => Write(LogEventLevel.Verbose, t);
    public void RequestResponseErrorLog<T>(T t) where T : IRequestResponseLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log) => Logger.Write(logLevel, "{@Log}", log);
}