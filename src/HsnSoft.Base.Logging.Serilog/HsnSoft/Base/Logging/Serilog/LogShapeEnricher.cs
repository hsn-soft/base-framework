using System;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Gives every log the two fields that sort logs in Graylog: <c>LogType</c> (<see cref="LogTypes"/>: the framework's, the platform's,
/// the application's, or one of the logs that follow a chain, which say it themselves) and <c>MessageType</c> (<c>Structured</c> when an
/// object was logged with <c>{@Object}</c>, <c>Args</c> when only plain values were). A log that already carries one is left as it is.
/// </summary>
internal sealed class LogShapeEnricher : ILogEventEnricher
{
    internal const string MessageTypeProperty = "MessageType";
    internal const string StructuredMessage = "Structured";
    internal const string ArgsMessage = "Args";

    private static readonly string[] s_platformCategories = ["System", "Microsoft", "Npgsql", "MongoDB", "Yarp", "Confluent", "RabbitMQ", "Grpc", "Azure", "StackExchange"];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogFields.LogType, LogType(SerilogRouting.SourceContext(logEvent))));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MessageTypeProperty, IsStructured(logEvent) ? StructuredMessage : ArgsMessage));
    }

    internal static string LogType(string category)
    {
        if (StartsWithCategory(category, "HsnSoft.Base"))
        {
            return LogTypes.FrameworkLog;
        }

        return s_platformCategories.Any(platform => StartsWithCategory(category, platform)) ? LogTypes.PlatformLog : LogTypes.AppLog;
    }

    // "Microsoft" is the category "Microsoft" and everything below it, not "MicrosoftExtras"
    private static bool StartsWithCategory(string category, string prefix)
        => category.StartsWith(prefix, StringComparison.Ordinal) && (category.Length == prefix.Length || category[prefix.Length] == '.');

    private static bool IsStructured(LogEvent logEvent)
        => logEvent.Properties.Any(property => property.Key != "EventId" && property.Value is StructureValue or DictionaryValue);
}
