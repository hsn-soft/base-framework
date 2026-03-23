using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.EventBus.Logging;

public sealed class EventBusLogger(IConfiguration configuration, ILogMasker masker) : PersistentLogger(configuration, "EventBusLogger"), IEventBusLogger
{
    public void EventBusInfoLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Information, "EventBusLog", log);

    public void EventBusWarnLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Warning, "EventBusLog", log);

    public void EventBusErrorLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Error, "EventBusLog", log);

    private void Write<T>(LogEventLevel logLevel, string logType, T log)
    {
        object masked = masker.MaskObject(log);

        Logger
            .ForContext("LogType", logType)
            .ForContext("Payload", masked, destructureObjects: true)
            .Write(logLevel, "{LogType} created", logType);
    }
}