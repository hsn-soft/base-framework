using System;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>Which sink a log event goes to. Pure functions of the event, so the rules are the same wherever they are used.</summary>
internal static class SerilogRouting
{
    internal static LogEventLevel ToSerilog(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        _ => LogEventLevel.Fatal,
    };

    /// <summary>The category the log was written under (<c>ILogger&lt;T&gt;</c> uses the name of <c>T</c>).</summary>
    internal static string SourceContext(LogEvent logEvent)
        => logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out LogEventPropertyValue? value) && value is ScalarValue { Value: string text }
            ? text
            : string.Empty;

    /// <summary>Logs of the console logger (<c>IAppConsoleLogger</c>) are never persistent.</summary>
    internal static bool IsConsoleOnly(LogEvent logEvent) => LogRouting.IsConsoleOnly(SourceContext(logEvent));

    internal static LogLevel ToLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        _ => LogLevel.Critical,
    };

    /// <summary>The console shows a log when its level reaches what <c>Logging:LogLevel</c> says for its category (nothing at all for <c>None</c>).</summary>
    internal static bool ForConsole(LogEvent logEvent, LogLevelRules rules) => LogRouting.ForConsole(ToLogLevel(logEvent.Level), SourceContext(logEvent), rules);

    /// <summary>
    /// The persistent sink (Graylog, file) gets a log when its level reaches what <see cref="PersistentLevelRules"/> says for its category (never below Information, so
    /// Debug and Verbose logs stay in the console whatever the configuration says) and it is not for the console only.
    /// </summary>
    internal static bool ForPersistent(LogEvent logEvent, PersistentLevelRules rules)
        => LogRouting.ForPersistent(ToLogLevel(logEvent.Level), SourceContext(logEvent), LogTypeOf(logEvent), rules);

    private static string LogTypeOf(LogEvent logEvent)
        => logEvent.Properties.TryGetValue(LogFields.LogType, out LogEventPropertyValue? type) && type is ScalarValue { Value: string text } ? text : string.Empty;

    /// <summary>The name shown next to the level: the console logger's own name, the structured logger's, or the host's.</summary>
    internal static string LoggerName(LogEvent logEvent, string hostName)
    {
        string source = SourceContext(logEvent);
        if (source.StartsWith(LogCategories.ConsoleOnlyPrefix, StringComparison.Ordinal))
        {
            return source[LogCategories.ConsoleOnlyPrefix.Length..];
        }

        return hostName;
    }
}
