using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Removes what is on a log and nobody searches for: the id of the TCP connection that ASP.NET Core puts on every log, and, from the logs of the framework and of the
/// platform, who asked (the user and the client of the request or message they happened in): that is for the logs of the request, of the message and of the application.
/// </summary>
internal sealed class NoiseEnricher : ILogEventEnricher
{
    internal const string ConnectionId = nameof(ConnectionId);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.RemovePropertyIfPresent(ConnectionId);

        if (logEvent.Properties.TryGetValue(LogFields.LogType, out LogEventPropertyValue? type) && type is ScalarValue { Value: LogTypes.FrameworkLog or LogTypes.PlatformLog })
        {
            foreach (string field in LogFields.WhoFields)
            {
                logEvent.RemovePropertyIfPresent(field);
            }
        }
    }
}
