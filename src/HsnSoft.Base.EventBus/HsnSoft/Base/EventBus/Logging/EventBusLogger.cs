using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.EventBus.Logging;

public sealed class EventBusLogger(IConfiguration configuration, ILogMasker masker) : PersistentLogger(configuration, nameof(EventBusLogger),true), IEventBusLogger
{
    public void EventBusInfoLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Information, "EventBusLog", log);

    public void EventBusWarnLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Warning, "EventBusLog", log);

    public void EventBusErrorLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Error, "EventBusLog", log);

    private void Write<T>(LogEventLevel logLevel, string logType, T log)
    {
        // object masked = masker.MaskObject(log);
        //
        // // birinci yöntem
        // Logger
        //     .ForContext("LogType", logType)
        //     .ForContext("Log", masked, destructureObjects: true)
        //     .Write(logLevel, "{logLevel} | {LogType} created", logLevel.ToString(),logType);

        // ikinci yöntem, serilog konfigurasyonunda maskeleme ekleniyor zaten
        Logger
            .ForContext("LogType", logType)
            .Write(logLevel, "{@Log}", log);
    }
}