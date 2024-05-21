using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace HsnSoft.Base.Logging;

public class DefaultBaseLogger : IBaseLogger
{
    protected readonly ILogger Logger;

    public DefaultBaseLogger(IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(configuration);

            try
            {
                var loglevel = (LogLevel)Enum.Parse(typeof(LogLevel), configuration["Logging:LogLevel:Default"] ?? throw new InvalidOperationException());
                builder.SetMinimumLevel(loglevel);
            }
            catch (Exception)
            {
                // no config
                builder.SetMinimumLevel(LogLevel.Information);
            }

            // Clear Microsoft's default providers (like event logs and others)
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
                options.ColorBehavior = LoggerColorBehavior.Default;
            });
        });

        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public void LogDebug(string messageTemplate, params object[] args) => Logger.LogDebug(messageTemplate, args);

    public void LogError(string messageTemplate, params object[] args) => Logger.LogError(messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) => Logger.LogWarning(messageTemplate, args);

    public void LogInformation(string messageTemplate, params object[] args) => Logger.LogInformation(messageTemplate, args);
}