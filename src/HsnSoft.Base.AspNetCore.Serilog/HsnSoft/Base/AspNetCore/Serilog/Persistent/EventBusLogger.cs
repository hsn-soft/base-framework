using HsnSoft.Base.EventBus.Logging;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog.Persistent;

public sealed class EventBusLogger : SerilogBaseLogger, IEventBusLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog => Write(LogEventLevel.Verbose, t);

    public void EventBusErrorLog<T>(T t) where T : IEventBusLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log) => BaseLogger.Write(logLevel, "{@Log}", log);
}