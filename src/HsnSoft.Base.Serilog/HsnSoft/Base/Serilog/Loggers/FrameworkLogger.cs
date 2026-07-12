using System;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Loggers;

public sealed class FrameworkLogger(IConfiguration configuration) : PersistentLogger(configuration, nameof(FrameworkLogger), true), IFrameworkLogger
{
    public void FrameworkInfoLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Information, log);

    public void FrameworkWarnLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Warning, log);

    public void FrameworkErrorLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Error, log);

    private void Write<T>(LogEventLevel logLevel, T log)
        => Logger
            .ForContext("LogType", "FrameworkLog")
            .ForContext("MessageType", "Structured")
            .Write(logLevel, "{@Log}", log);

    protected override void Write(LogEventLevel logLevel, Exception exception, string messageTemplate, params object[] args)
        => Logger
            .ForContext("LogType", "FrameworkLog")
            .ForContext("MessageType", "Args")
            .Write(logLevel, exception, messageTemplate, args);
}