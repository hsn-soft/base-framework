using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Lets a product add what only it knows to the request and response logs: the client's channel and version, the device, the user's
/// roles, a location. Register it as a singleton; it is called for both logs, the <c>logType</c> argument says which
/// (<c>HttpRequest</c> or <c>HttpResponse</c>). The values become fields of the log, so keep their number and their length fixed.
/// </summary>
public interface IRequestLogEnricher
{
    void Enrich(HttpContext context, string logType, IDictionary<string, object?> fields);
}
