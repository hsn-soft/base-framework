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
    internal static bool IsConsoleOnly(LogEvent logEvent)
        => SourceContext(logEvent).StartsWith(LogCategories.ConsoleOnlyPrefix, StringComparison.Ordinal);

    /// <summary>Structured logs (<c>IFrameworkLogger</c>) are printed with their properties.</summary>
    internal static bool ShowsProperties(LogEvent logEvent)
        => string.Equals(SourceContext(logEvent), LogCategories.FrameworkLog, StringComparison.Ordinal);

    /// <summary>The console shows everything from the configured level up.</summary>
    internal static bool ForConsole(LogEvent logEvent, LogEventLevel minimum) => logEvent.Level >= minimum;

    /// <summary>
    /// The persistent sink (Graylog, file) gets what is at least at the persistent level and is not for the console only. The level
    /// is never below Information, so Debug and Verbose logs stay in the console whatever the configuration says.
    /// </summary>
    internal static bool ForPersistent(LogEvent logEvent, LogEventLevel persistentMinimum)
        => logEvent.Level >= persistentMinimum && !IsConsoleOnly(logEvent);

    /// <summary>The name shown next to the level: the console logger's own name, the structured logger's, or the host's.</summary>
    internal static string LoggerName(LogEvent logEvent, string hostName)
    {
        string source = SourceContext(logEvent);
        if (source.StartsWith(LogCategories.ConsoleOnlyPrefix, StringComparison.Ordinal))
        {
            return source[LogCategories.ConsoleOnlyPrefix.Length..];
        }

        return string.Equals(source, LogCategories.FrameworkLog, StringComparison.Ordinal) ? "FrameworkLogger" : hostName;
    }
}
