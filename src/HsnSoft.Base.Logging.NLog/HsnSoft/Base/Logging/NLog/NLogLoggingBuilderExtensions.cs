using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Common;
using NLog.Config;
using NLog.Extensions.Logging;

namespace HsnSoft.Base.Logging.NLog;

public static class NLogLoggingBuilderExtensions
{
    /// <summary>
    /// Makes NLog the logging provider of the service, with the same settings, routing and fields as <c>AddBaseSerilog</c> (a service moves from one to the other by changing this call): everything that logs through
    /// <c>ILogger</c> (the HsnSoft packages, ASP.NET Core, EF Core) goes through it, as does <c>IAppConsoleLogger</c>. Two sets of settings (see <see cref="PersistentLoggerOptions"/>):
    /// <list type="bullet">
    /// <item>The console shows what the standard <c>Logging:LogLevel</c> says: a default and a level per category.</item>
    /// <item>The persistent sink (Graylog over HTTP, or a file when Graylog is off) gets what <c>PersistentLogger:LogLevel</c> says (never below Information), except what <c>IAppConsoleLogger</c> wrote. Debug and Trace logs never leave the console.</item>
    /// <item>The persistent log has the fields of the framework (<c>LogType</c>, <c>MessageType</c>, <c>Facility</c>, ...), a bounded number of them, and what is marked <c>[SensitiveData]</c> is masked in the message and in the fields.</item>
    /// <item>Graylog is written by a queue in the background that sends again what failed; there is no sink to rebuild, so <c>GrayLog:SinkRefreshIntervalMinutes</c> is not used.</item>
    /// </list>
    /// The service has its own NLog factory: nothing of the global <c>LogManager</c> is touched, and an <c>nlog.config</c> file is not read.
    /// </summary>
    /// <param name="builder">The logging builder (<c>builder.Logging</c>). Other providers are removed.</param>
    /// <param name="configuration">Holds the <c>Logging:LogLevel</c> and <c>PersistentLogger</c> sections.</param>
    /// <param name="loggerName">Shown next to the level in the console and the file; the host's name.</param>
    /// <param name="configure">Changes the NLog configuration before the logger is created, e.g. to add a target of the product.</param>
    public static ILoggingBuilder AddBaseNLog(this ILoggingBuilder builder, IConfiguration configuration, string loggerName = "Host", Action<LoggingConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(configuration);
        if (options.IsSelfLogActive)
        {
            // NLog swallows the errors of its targets on purpose (an unreachable Graylog must not break the application), so without this they leave no trace
            InternalLogger.LogLevel = global::NLog.LogLevel.Warn;
            InternalLogger.LogToConsoleError = true;
        }

        BaseNLogSetup setup = BaseNLog.Create(options, loggerName, configure);

        builder.ClearProviders();

        // NLog decides what is written, per place: the console by Logging:LogLevel and the persistent sink by PersistentLogger. The filter of
        // Microsoft.Extensions.Logging is opened wide, or it would decide for both and hide from the persistent sink what the console does not show
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddProvider(new NLogLoggerProvider(
            new NLogProviderOptions
            {
                IncludeScopes = true,
                CaptureMessageTemplates = true,
                CaptureMessageProperties = true,
                ParseMessageTemplates = true,
                ShutdownOnDispose = true,
            },
            setup.Factory));
        return builder;
    }
}
