using System;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Loggers;

public abstract class PersistentLogger : IBaseLogger
{
    protected readonly ILogger Logger;

    protected PersistentLogger(IConfiguration configuration, string loggerName,bool usePropertyConsoleTemplate)
    {
        try
        {
            Logger = SerilogConfigurationHelper
                .ConfigureConsoleWithPersistentLogger(configuration, loggerName)
                .ForContext("IsPersistentLogger", true)
                .ForContext("UsePropertyConsole", usePropertyConsoleTemplate);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"LogManager is not initialized, please configure appsettings.json. Ex: {exception}");

            Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {LoggerName} [{SourceContext}] | {Message:lj}{NewLine}{Exception}{NewLine}")
                .CreateLogger()
                .ForContext("LoggerName", loggerName)
                .ForContext("IsPersistentLogger", true);
        }
    }

    public void LogDebug(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Debug, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Information, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Warning, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Error, messageTemplate, args);

    public void LogError(Exception exception, string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Error, exception, messageTemplate, args);
}