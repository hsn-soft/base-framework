using System;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.EventBus.Logging;

public sealed class EventBusLogger(IConfiguration configuration) : PersistentLogger(configuration, nameof(EventBusLogger), true), IEventBusLogger
{
    public void EventBusInfoLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Information, log);

    public void EventBusWarnLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Warning, log);

    public void EventBusErrorLog<T>(T log) where T : IEventBusLog => Write(LogEventLevel.Error, log);

    private void Write<T>(LogEventLevel logLevel, T log)
        => Logger
            .ForContext("LogType", "EventBusLog")
            .ForContext("MessageType", "Structured")
            .Write(logLevel, "{@Log}", log);

    protected override void Write(LogEventLevel logLevel, Exception exception, string messageTemplate, params object[] args)
        => Logger
            .ForContext("LogType", "EventBusLog")
            .ForContext("MessageType", "Args")
            .Write(logLevel, exception, messageTemplate, args);
}