using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

public static class LoggerFacilityExtensions
{
    /// <summary>
    /// Names what the logs written until the returned scope is disposed are about (<c>ORDER_IMPORTED</c>): their <c>Facility</c> in Graylog, to search for and to
    /// see the flow of an operation by. A single log can also name itself with the name of its event (<c>[LoggerMessage(EventName = "ORDER_IMPORTED")]</c>,
    /// in capitals).
    /// </summary>
    /// <example><code>using (logger.BeginFacility("ORDER_IMPORTED")) { logger.LogInformation("Order {OrderId} imported", id); }</code></example>
    public static IDisposable? BeginFacility(this ILogger logger, string facility)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(facility);

        return logger.BeginScope(new Dictionary<string, object> { [LogFields.Facility] = facility });
    }
}
