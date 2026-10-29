using System;
using NLog;
using NLog.Config;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;

namespace HsnSoft.Base.Logging.NLog;

/// <summary>The NLog configuration of a service and the factory that holds it.</summary>
/// <param name="Factory">The log factory (the service's own: no global configuration is touched).</param>
/// <param name="Shape">The target in front of the persistent sink.</param>
internal sealed record BaseNLogSetup(LogFactory Factory, LogShapeTarget Shape);

internal static class BaseNLog
{
    /// <param name="options">The settings of the logging.</param>
    /// <param name="loggerName">The name of the host.</param>
    /// <param name="configure">Changes the configuration before the factory is made.</param>
    /// <param name="console">The console target; a test gives its own (it gets the layout of the console).</param>
    /// <param name="persistent">The target behind the shaping; a test gives its own.</param>
    internal static BaseNLogSetup Create(PersistentLoggerOptions options, string loggerName, Action<LoggingConfiguration>? configure, Target? console = null, Target? persistent = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        LogLevelRules consoleLevels = options.ConsoleLevels;
        PersistentLevelRules persistentLevels = new(options);

        Layout consoleLayout = Layout.FromMethod(logEvent => NLogText.Console(logEvent, loggerName));
        Target consoleTarget = console ?? new ColoredConsoleTarget("base-console") { DetectOutputRedirected = true };
        if (consoleTarget is TargetWithLayout withLayout)
        {
            withLayout.Layout = consoleLayout;
        }

        LogShapeTarget shape = new(persistent ?? PersistentTarget(options, loggerName), options, loggerName);

        LoggingConfiguration configuration = new();
        configuration.AddTarget(consoleTarget);
        configuration.AddTarget(shape);
        LoggingRule consoleRule = new("*", NLogLevel.Trace, NLogLevel.Fatal, consoleTarget);
        consoleRule.Filters.Add(new WhenMethodFilter(logEvent => ForConsole(logEvent, consoleLevels) ? FilterResult.Log : FilterResult.Ignore));
        configuration.AddRule(consoleRule);

        LoggingRule persistentRule = new("*", NLogLevel.Trace, NLogLevel.Fatal, shape);
        persistentRule.Filters.Add(new WhenMethodFilter(logEvent => ForPersistent(logEvent, persistentLevels, shape) ? FilterResult.Log : FilterResult.Ignore));
        configuration.AddRule(persistentRule);

        configure?.Invoke(configuration);
        return new BaseNLogSetup(new LogFactory { Configuration = configuration }, shape);
    }

    internal static LogLevel ToLogLevel(NLogLevel level)
        => level.Ordinal switch
        {
            0 => LogLevel.Trace,
            1 => LogLevel.Debug,
            2 => LogLevel.Information,
            3 => LogLevel.Warning,
            4 => LogLevel.Error,
            _ => LogLevel.Critical,
        };

    internal static bool ForConsole(LogEventInfo logEvent, LogLevelRules rules)
        => LogRouting.ForConsole(ToLogLevel(logEvent.Level), logEvent.LoggerName ?? string.Empty, rules);

    internal static bool ForPersistent(LogEventInfo logEvent, PersistentLevelRules rules, LogShapeTarget shape)
    {
        string category = logEvent.LoggerName ?? string.Empty;
        return LogRouting.ForPersistent(ToLogLevel(logEvent.Level), category, shape.LogType(category), rules);
    }

    private static Target PersistentTarget(PersistentLoggerOptions options, string hostName)
    {
        if (options.IsGrayLogActive)
        {
            if (string.IsNullOrWhiteSpace(options.GrayLog.Address))
            {
                throw new InvalidOperationException($"{PersistentLoggerOptions.SectionName}:GrayLog:Address is required when {PersistentLoggerOptions.SectionName}:IsGrayLogActive is true.");
            }

            return new GelfHttpTarget(options.GrayLog.Address, options.GrayLog.Port, options.GrayLog.UseHttps);
        }

        return new FileTarget("base-file")
        {
            FileName = options.FilePath,
            Layout = Layout.FromMethod(logEvent => NLogText.File(logEvent, hostName)),
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 30,
            KeepFileOpen = true,
        };
    }
}
