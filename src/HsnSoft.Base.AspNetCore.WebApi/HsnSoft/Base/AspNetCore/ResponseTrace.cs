using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// The <c>TraceId</c> of an answer: the same value the logs of the request have as <c>TraceId</c> (the W3C trace the gateway passes on, so the logs
/// of the gateway and of the services all have it), so what a client reads out of an answer finds its logs. Without a trace, the id ASP.NET Core
/// gives the request (the <c>RequestId</c> of its logs).
/// </summary>
internal static class ResponseTrace
{
    public static string Of(HttpContext context)
        => Activity.Current?.TraceId.ToHexString() is { } traceId && traceId != new string('0', 32) ? traceId : context.TraceIdentifier;
}
