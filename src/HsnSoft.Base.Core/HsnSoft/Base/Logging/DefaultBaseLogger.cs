using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace HsnSoft.Base.Logging;

public class DefaultBaseLogger : IBaseLogger
{
    protected readonly ILogger BaseLogger;

    public DefaultBaseLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.ClearProviders();

            // Clear Microsoft's default providers (like event logs and others)
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
                options.ColorBehavior = LoggerColorBehavior.Default;
            });
        });

        BaseLogger = loggerFactory.CreateLogger(GetType().FullName);
    }

    public void LogDebug(string messageTemplate, params object[] args) => BaseLogger.LogDebug(messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => BaseLogger.LogError(messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => BaseLogger.LogWarning(messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => BaseLogger.LogInformation(messageTemplate, args);
}