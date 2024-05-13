using System;
using HsnSoft.Base.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace HsnSoft.Base.AspNetCore.Serilog;

public class SerilogBaseLogger : IBaseLogger
{
    protected readonly ILogger BaseLogger;

    public SerilogBaseLogger()
    {
        try
        {
            BaseLogger = new LoggerConfiguration()
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

    public void LogDebug(string messageTemplate, params object[] args) => BaseLogger.Write(LogEventLevel.Debug, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => BaseLogger.Write(LogEventLevel.Error, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => BaseLogger.Write(LogEventLevel.Warning, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => BaseLogger.Write(LogEventLevel.Information, messageTemplate, args);
}