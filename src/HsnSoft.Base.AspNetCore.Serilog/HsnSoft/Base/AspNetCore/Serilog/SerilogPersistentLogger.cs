using System;
using HsnSoft.Base.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace HsnSoft.Base.AspNetCore.Serilog;

public sealed class SerilogPersistentLogger<T> : IPersistentLogger<T> where T : IPersistentLog
{
    private readonly ILogger _logger;

    public SerilogPersistentLogger()
    {
        try
        {
            _logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
                .MinimumLevel.Verbose()
                .WriteTo.Conditional(logEvent => logEvent is { Level: LogEventLevel.Verbose or LogEventLevel.Fatal },
                    sinkConfiguration => sinkConfiguration.File("Logs/logs.txt")
                )
                .WriteTo.Async(c => c.Console // All logs , Verbose,Debug,Information, Warning, Error, Fatal
                (
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    theme: AnsiConsoleTheme.Code
                ))
                .CreateLogger();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"LogManager is not initialized, please configure appsettings.json. Ex: {exception}");
        }
    }

    public void LogDebug(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Debug, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Error, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Warning, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Information, messageTemplate, args);

    public void PersistentInfoLog(T t) => Write(LogEventLevel.Verbose, t);

    public void PersistentErrorLog(T t) => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T Log) => _logger.Write(logLevel, "{@Log}", Log);
}