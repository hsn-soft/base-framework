using System;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class RequestResponseLogger(IConfiguration configuration) : PersistentLogger(configuration, nameof(RequestResponseLogger), true), IRequestResponseLogger
{
    public void RequestResponseInfoLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Information, log);

    public void RequestResponseWarnLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Warning, log);

    public void RequestResponseErrorLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Error, log);

    private void Write<T>(LogEventLevel logLevel, T log)
        => Logger
            .ForContext("LogType", "RequestResponseLog")
            .ForContext("MessageType", "Structured")
            .Write(logLevel, "{@Log}", log);

    protected override void Write(LogEventLevel logLevel, Exception exception, string messageTemplate, params object[] args)
        => Logger
            .ForContext("LogType", "RequestResponseLog")
            .ForContext("MessageType", "Args")
            .Write(logLevel, exception, messageTemplate, args);
}