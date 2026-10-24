using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>Removes what ASP.NET Core puts on every log and nobody searches for (the id of the TCP connection), so it is not a field of every log.</summary>
internal sealed class NoiseEnricher : ILogEventEnricher
{
    internal const string ConnectionId = nameof(ConnectionId);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) => logEvent.RemovePropertyIfPresent(ConnectionId);
}
