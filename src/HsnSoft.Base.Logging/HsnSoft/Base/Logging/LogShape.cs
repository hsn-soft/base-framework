using System;
using System.Collections.Generic;
using System.Linq;

namespace HsnSoft.Base.Logging;

/// <summary>
/// What makes a log of the framework a log of the framework, whichever logging library writes it: the type of a log told from its category, the <c>Facility</c> told from the name of its event, and the fields nobody searches
/// for. A provider (Serilog, NLog) puts these on a log as properties; the rules are here so every provider gives the same fields.
/// </summary>
public static class LogShape
{
    /// <summary>The field that says whether an object was logged with <c>{@Object}</c>: <see cref="StructuredMessage"/>, or <see cref="ArgsMessage"/> when only plain values were.</summary>
    public const string MessageTypeField = "MessageType";

    public const string StructuredMessage = "Structured";

    public const string ArgsMessage = "Args";

    /// <summary>The field that says what a log is about (<c>CONSUME_EVENT_SUCCESS</c>).</summary>
    public const string FacilityField = "Facility";

    /// <summary>The id of the TCP connection that ASP.NET Core puts on every log.</summary>
    public const string ConnectionIdField = "ConnectionId";

    private const string FrameworkNamespace = "HsnSoft.Base";
    private const string SharedNamespacePart = "Shared";

    private static readonly string[] s_notForAppLogs = ["ActionId", "ActionName", "ParentId", "RequestId", "RequestPath", "SpanId"];

    /// <summary>
    /// The <see cref="LogTypes"/> of a log, from the category it was written under (the class that wrote it): <c>HsnSoft.Base.*</c> is the framework's, what starts with <paramref name="appNamespace"/> is the application's,
    /// <c>{appNamespace}.Shared.*</c> is its shared library's, a log without a category (or the console logger's) is the application's, and everything else is the platform's: no list of libraries to keep up to date.
    /// </summary>
    public static string LogType(string category, string appNamespace)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(appNamespace);

        if (category.Length == 0 || category.StartsWith(LogCategories.ConsoleOnlyPrefix, StringComparison.Ordinal))
        {
            return LogTypes.AppLog;
        }

        if (StartsWithCategory(category, FrameworkNamespace))
        {
            return LogTypes.FrameworkLog;
        }

        if (StartsWithCategory(category, $"{appNamespace}.{SharedNamespacePart}"))
        {
            return LogTypes.SharedLog;
        }

        return StartsWithCategory(category, appNamespace) ? LogTypes.AppLog : LogTypes.PlatformLog;
    }

    /// <summary>The <c>Facility</c> of a log: the name of its event when it is written in capitals (<c>[LoggerMessage(EventName = "CONSUME_EVENT_SUCCESS")]</c>); the names the platform gives its events (<c>ExecutedEndpoint</c>) are not facilities.</summary>
    public static string? FacilityName(string? eventName)
        => !string.IsNullOrEmpty(eventName) && eventName.All(character => character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_') ? eventName : null;

    /// <summary>
    /// The fields to take off a log of the given type: the id of the connection always; from the logs of the framework and of the platform, who asked (that is for the logs of the request, of the message and of the
    /// application); from the logs of the application and of its shared library, the endpoint, the span and the request they happened in (ASP.NET Core and the tracing put them on every log).
    /// </summary>
    public static IEnumerable<string> NoiseFields(string logType)
    {
        yield return ConnectionIdField;

        IEnumerable<string> more = logType switch
        {
            LogTypes.FrameworkLog or LogTypes.PlatformLog => LogFields.WhoFields,
            LogTypes.AppLog or LogTypes.SharedLog => s_notForAppLogs,
            _ => [],
        };

        foreach (string field in more)
        {
            yield return field;
        }
    }

    // "Hhs" is the category "Hhs" and everything below it, not "HhsExtras"
    private static bool StartsWithCategory(string category, string prefix)
        => category.StartsWith(prefix, StringComparison.Ordinal) && (category.Length == prefix.Length || category[prefix.Length] == '.');
}
