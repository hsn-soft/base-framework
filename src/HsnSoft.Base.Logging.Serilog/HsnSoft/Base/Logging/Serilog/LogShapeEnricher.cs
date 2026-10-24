using System;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Gives every log the two fields that sort logs in Graylog: <c>LogType</c> (<see cref="LogTypes"/>: the framework's, the application's, the platform's,
/// or one of the logs that follow a chain, which say it themselves) and <c>MessageType</c> (<c>Structured</c> when an
/// object was logged with <c>{@Object}</c>, <c>Args</c> when only plain values were). A log that already carries one is left as it is.
/// The type is told from the category the log was written under (the class that wrote it): <c>HsnSoft.Base.*</c> is the framework's, what starts with the
/// application's namespace (<c>FrameworkLogger:AppNamespace</c>) is the application's, <c>{AppNamespace}.Shared.*</c> is its shared library's, and everything else is the platform's:
/// no list of libraries to keep up to date.
/// </summary>
/// <param name="appNamespace">The start of the categories of the application (<c>Hhs</c>).</param>
internal sealed class LogShapeEnricher(string appNamespace) : ILogEventEnricher
{
    internal const string MessageTypeProperty = "MessageType";
    internal const string StructuredMessage = "Structured";
    internal const string ArgsMessage = "Args";

    private const string FrameworkNamespace = "HsnSoft.Base";
    private const string SharedNamespacePart = "Shared";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogFields.LogType, LogType(SerilogRouting.SourceContext(logEvent))));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MessageTypeProperty, IsStructured(logEvent) ? StructuredMessage : ArgsMessage));
    }

    internal string LogType(string category)
    {
        // a log without a category is written by the application itself (the static Log); the console logger's category is the application's too
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

    // "Hhs" is the category "Hhs" and everything below it, not "HhsExtras"
    private static bool StartsWithCategory(string category, string prefix)
        => category.StartsWith(prefix, StringComparison.Ordinal) && (category.Length == prefix.Length || category[prefix.Length] == '.');

    private static bool IsStructured(LogEvent logEvent)
        => logEvent.Properties.Any(property => property.Key != "EventId" && property.Value is StructureValue or DictionaryValue);
}
