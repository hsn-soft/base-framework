using System;
using System.Globalization;
using System.Text;
using NLog;

namespace HsnSoft.Base.Logging.NLog;

/// <summary>The lines of the console and of the file: the same as the Serilog provider writes.</summary>
internal static class NLogText
{
    /// <summary><c>[10:15:30.123 +03:00 INF] Host | message</c> and the exception below it.</summary>
    internal static string Console(LogEventInfo logEvent, string hostName)
    {
        StringBuilder line = new();
        line.Append('[').Append(logEvent.TimeStamp.ToString("HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)).Append(' ').Append(Level(logEvent.Level)).Append("] ")
            .Append(LoggerName(logEvent, hostName)).Append(" | ").Append(NLogMessage.Render(logEvent));
        AppendException(line, logEvent);
        return line.ToString();
    }

    /// <summary><c>[2026-10-27 10:15:30.123 +03:00 INF] Host [Category] | message</c> and the exception below it.</summary>
    internal static string File(LogEventInfo logEvent, string hostName)
    {
        StringBuilder line = new();
        line.Append('[').Append(logEvent.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)).Append(' ').Append(Level(logEvent.Level)).Append("] ")
            .Append(LoggerName(logEvent, hostName)).Append(" [").Append(logEvent.LoggerName).Append("] | ").Append(NLogMessage.Render(logEvent));
        AppendException(line, logEvent);
        return line.ToString();
    }

    internal static string LoggerName(LogEventInfo logEvent, string hostName)
        => LogRouting.IsConsoleOnly(logEvent.LoggerName ?? string.Empty) ? logEvent.LoggerName![LogCategories.ConsoleOnlyPrefix.Length..] : hostName;

    private static string Level(LogLevel level)
        => level.Ordinal switch
        {
            0 => "TRC",
            1 => "DBG",
            2 => "INF",
            3 => "WRN",
            4 => "ERR",
            _ => "FTL",
        };

    private static void AppendException(StringBuilder line, LogEventInfo logEvent)
    {
        if (logEvent.Exception is not null)
        {
            line.Append(Environment.NewLine).Append(logEvent.Exception);
        }
    }
}
