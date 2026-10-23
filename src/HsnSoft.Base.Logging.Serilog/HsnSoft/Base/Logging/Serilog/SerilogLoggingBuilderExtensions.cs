using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HsnSoft.Base.Logging.Serilog;

public static class SerilogLoggingBuilderExtensions
{
    /// <summary>
    /// Makes Serilog the logging provider of the service, configured from the <c>FrameworkLogger</c> section (see
    /// <see cref="FrameworkLoggerOptions"/>). Everything that logs through <c>ILogger</c> (the HsnSoft packages, ASP.NET Core, EF Core) goes
    /// through it, as do <c>IAppConsoleLogger</c> and <c>IFrameworkLogger</c>.
    /// <list type="bullet">
    /// <item>The console shows everything from <c>FrameworkLogger:LogLevel</c> up.</item>
    /// <item>The persistent sink (Graylog, or a file when Graylog is off) gets Information and above, except what <c>IAppConsoleLogger</c> wrote. Debug and Verbose logs never leave the console.</item>
    /// <item>Sensitive data (<c>[SensitiveData]</c> properties) is masked when an object is logged with <c>{@Object}</c>.</item>
    /// <item>The Graylog sink is rebuilt every <c>FrameworkLogger:GrayLog:SinkRefreshIntervalMinutes</c>.</item>
    /// </list>
    /// </summary>
    /// <param name="builder">The logging builder (<c>builder.Logging</c>). Other providers are removed.</param>
    /// <param name="configuration">Holds the <c>FrameworkLogger</c> section.</param>
    /// <param name="loggerName">Shown next to the level in the console and the file; the host's name.</param>
    /// <param name="configure">Changes the Serilog configuration before the logger is created, e.g. to add <c>Destructure.JsonNetTypes()</c> for a product that logs Newtonsoft objects.</param>
    /// <param name="replaceGlobalLogger">Also assigns the logger to <c>Log.Logger</c>, for code that uses Serilog's static <c>Log</c>.</param>
    public static ILoggingBuilder AddBaseSerilog(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        string loggerName = "Host",
        Action<LoggerConfiguration>? configure = null,
        bool replaceGlobalLogger = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(configuration);
        BaseSerilogSetup setup = BaseSerilog.Create(options, loggerName, configure);

        builder.ClearProviders();
        // Serilog decides what is written; the default filter of Microsoft.Extensions.Logging (Information) would hide Debug logs first
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddSerilog(setup.Logger, dispose: true);

        if (setup.Refreshable is not null)
        {
            TimeSpan interval = TimeSpan.FromMinutes(options.GrayLog.SinkRefreshIntervalMinutes);
            builder.Services.AddSingleton<IHostedService>(provider => new GraylogSinkRefreshService(
                setup.Refreshable,
                interval,
                provider.GetRequiredService<ILogger<GraylogSinkRefreshService>>()));
        }

        if (replaceGlobalLogger)
        {
            Log.Logger = setup.Logger;
        }

        return builder;
    }
}
