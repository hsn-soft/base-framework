using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Makes <c>Facility</c> (what the log is about: <c>CONSUME_EVENT_SUCCESS</c>, <c>APPLICATION_LOADER_STARTED</c>) and
/// <c>CorrelationId</c> top-level properties, so Graylog can search them as fields. A log has them in two places: the name of its
/// event, when it is written in capitals (<c>[LoggerMessage(EventName = "...")]</c>) and the <c>Facility</c> and <c>CorrelationId</c> of a structured log written
/// with <c>IFrameworkLogger</c>; a property the log already carries on its own is left as it is.
/// </summary>
internal sealed class FacilityEnricher : ILogEventEnricher
{
    internal const string FacilityProperty = "Facility";
    internal const string CorrelationIdProperty = "CorrelationId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.ContainsKey(FacilityProperty))
        {
            string? facility = Text(Member(logEvent, "Log", FacilityProperty)) ?? FacilityName(Text(Member(logEvent, "EventId", "Name")));
            if (facility is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(FacilityProperty, facility));
            }
        }

        if (!logEvent.Properties.ContainsKey(CorrelationIdProperty) && Text(Member(logEvent, "Log", CorrelationIdProperty)) is { } correlationId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CorrelationIdProperty, correlationId));
        }
    }

    private static LogEventPropertyValue? Member(LogEvent logEvent, string property, string member)
        => logEvent.Properties.TryGetValue(property, out LogEventPropertyValue? value) && value is StructureValue structure
            ? structure.Properties.FirstOrDefault(candidate => candidate.Name == member)?.Value
            : null;

    // only the SCREAMING_SNAKE_CASE names are facilities; the names the platform gives its events (ExecutedEndpoint) are not
    private static string? FacilityName(string? eventName)
        => eventName is not null && eventName.All(character => character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_') ? eventName : null;

    private static string? Text(LogEventPropertyValue? value)
        => value is ScalarValue { Value: string { Length: > 0 } text } ? text : null;
}
