

using System;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Loggers;

public sealed class AppLogger : IAppConsoleLogger
{
    private readonly ILogger _logger;

    public AppLogger(IConfiguration configuration, string loggerName = "AppLogger")
    {
        try
        {
            _logger = SerilogConfigurationHelper
                .ConfigureConsoleLogger(configuration, loggerName)
                .ForContext("IsCustomLogger", true);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"LogManager is not initialized, please configure appsettings.json. Ex: {exception}");

            _logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {LoggerName} | {Message:lj}{NewLine}{Exception}{NewLine}")
                .CreateLogger()
                .ForContext("LoggerName", loggerName)
                .ForContext("IsCustomLogger", true);
        }
    }

    public void LogDebug(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Debug, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Information, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Warning, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Error, messageTemplate, args);

    public void LogError(Exception exception, string messageTemplate, params object[] args) => _logger.Write(LogEventLevel.Error, exception, messageTemplate, args);
}