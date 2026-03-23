using System;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.Serilog;

public class BaseLogger : IBaseLogger
{
    protected readonly ILogger Logger;

    public BaseLogger(IConfiguration configuration)
    {
        try
        {
            Logger = SerilogConfigurationHelper.ConfigureFilePersistentLogger(configuration);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"LogManager is not initialized, please configure appsettings.json. Ex: {exception}");
        }
    }

    public void LogDebug(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Debug, messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Error, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Warning, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => Logger.Write(LogEventLevel.Information, messageTemplate, args);
}