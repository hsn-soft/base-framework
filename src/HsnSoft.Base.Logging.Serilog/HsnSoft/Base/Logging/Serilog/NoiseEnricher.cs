using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Removes what is on a log and nobody searches for: the id of the TCP connection that ASP.NET Core puts on every log, and, from the logs of the framework and of the
/// platform, who asked (the user and the client of the request or message they happened in): that is for the logs of the request, of the message and of the application.
/// An application log (and a shared library's) keeps what its developer wrote and the ids of its chain: the endpoint, the id of the span and the request it happened in
/// (which ASP.NET Core and the tracing put on every log) are left to the logs of the request and of the platform.
/// </summary>
internal sealed class NoiseEnricher : ILogEventEnricher
{
    internal const string ConnectionId = LogShape.ConnectionIdField;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.RemovePropertyIfPresent(ConnectionId);

        if (!logEvent.Properties.TryGetValue(LogFields.LogType, out LogEventPropertyValue? type) || type is not ScalarValue { Value: string logType })
        {
            return;
        }

        foreach (string field in LogShape.NoiseFields(logType))
        {
            logEvent.RemovePropertyIfPresent(field);
        }
    }
}
