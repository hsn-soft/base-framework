using System;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Loggers;

public abstract class PersistentLogger : IBaseLogger, IRefreshableLogger
{
    protected volatile ILogger Logger;

    private readonly IConfiguration _configuration;
    private readonly string _loggerName;
    private readonly bool _usePropertyConsoleTemplate;

    protected PersistentLogger(IConfiguration configuration, string loggerName, bool usePropertyConsoleTemplate)
    {
        _configuration = configuration;
        _loggerName = loggerName;
        _usePropertyConsoleTemplate = usePropertyConsoleTemplate;

        Logger = BuildLogger();
    }

    // Rebuilds the sink pipeline from scratch and swaps it in, disposing the old one. Some
    // third-party sinks (e.g. Serilog.Sinks.Graylog.Core's HttpTransportClient) have an unlocked
    // lazy-init race that can leave the transport permanently unconfigured for the rest of the
    // process's life if lost once at startup; periodically calling this (see
    // PersistentLoggerSinkRefreshWorker) bounds the outage to one refresh interval instead of
    // requiring a manual restart.
    public void RefreshSink()
    {
        ILogger oldLogger = Logger;
        Logger = BuildLogger();
        (oldLogger as IDisposable)?.Dispose();
    }

    private ILogger BuildLogger()
    {
        try
        {
            return SerilogConfigurationHelper
                .ConfigureConsoleWithPersistentLogger(_configuration, _loggerName)
                .ForContext("IsPersistentLogger", true)
                .ForContext("UsePropertyConsole", _usePropertyConsoleTemplate);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"LogManager is not initialized, please configure appsettings.json. Ex: {exception}");

            return new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {LoggerName} [{SourceContext}] | {Message:lj}{NewLine}{Exception}{NewLine}")
                .CreateLogger()
                .ForContext("LoggerName", _loggerName)
                .ForContext("IsPersistentLogger", true);
        }
    }


    protected abstract void Write(LogEventLevel logLevel,[CanBeNull] Exception exception, [NotNull] string messageTemplate, [ItemCanBeNull] params object[] args);


    public void LogDebug(string messageTemplate, params object[] args) => Write(LogEventLevel.Debug,null, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => Write(LogEventLevel.Information,null, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => Write(LogEventLevel.Warning,null, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => Write(LogEventLevel.Error,null, messageTemplate, args);

    public void LogError(Exception exception, string messageTemplate, params object[] args) => Write(LogEventLevel.Error, exception, messageTemplate, args);
}