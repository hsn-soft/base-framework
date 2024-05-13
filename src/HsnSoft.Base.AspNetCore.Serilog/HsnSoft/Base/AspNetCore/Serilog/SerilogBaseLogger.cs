using System;
using HsnSoft.Base.Logging;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog;

public class SerilogBaseLogger : IBaseLogger
{
    protected readonly ILogger BaseLogger;

    public SerilogBaseLogger()
    {
        try
        {
            BaseLogger = SerilogConfigurationHelper.ConfigureFilePersistentLogger();
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