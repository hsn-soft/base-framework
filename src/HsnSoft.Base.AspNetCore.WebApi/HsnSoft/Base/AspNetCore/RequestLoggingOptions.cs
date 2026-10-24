using System.Collections.Generic;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Who writes the request and response logs of a system with a gateway.</summary>
public enum RequestLoggingRole
{
    /// <summary>A service: logs every request it gets (<c>HttpRequest</c> when it arrives, <c>HttpResponse</c> when it is answered) and says so in the answer.</summary>
    Service,

    /// <summary>
    /// A gateway: leaves the logging to the services behind it, and logs one <c>HttpRequestResponse</c> only for what it answered itself
    /// (the service did not say it logged the request): a rejected token, a rate limit, an unknown route, a service that cannot be reached.
    /// </summary>
    Gateway,
}

/// <summary>What the request logging middleware writes (section <c>RequestLogging</c>).</summary>
public sealed class RequestLoggingOptions
{
    public const string SectionName = "RequestLogging";

    public RequestLoggingRole Role { get; set; } = RequestLoggingRole.Service;

    /// <summary>
    /// A service that logs, tells it in this response header (value: its name) so the gateway in front of it stays quiet. A request the
    /// service did not log (the log is off, the service is down) is logged by the gateway: a log twice is better than none.
    /// </summary>
    public string LoggedByHeader { get; set; } = "X-Logged-By";

    /// <summary>Turns the request and response logs of a service off (the correlation id is still set, and the gateway logs what the service did not).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Health probes are called every few seconds; their logs are off unless this is on.</summary>
    public bool LogHealthChecks { get; set; }

    /// <summary>Paths (prefixes) that are never logged, e.g. <c>/openapi</c>.</summary>
    public List<string> ExcludedPaths { get; set; } = ["/openapi", "/swagger"];

    /// <summary>
    /// Writes the JSON bodies of requests and responses. Off by default; a body is only ever written masked, so it also needs an
    /// <c>ILogMasker</c> (<c>AddBaseLogging()</c>): without one no body is logged. A gateway writes the body of a request only when the
    /// answer is a 5xx (what was asked when the service could not be reached or died), and keeps the request in a buffer for that.
    /// </summary>
    public bool LogBodies { get; set; }

    /// <summary>The longest body written (characters, after masking); a longer one is cut and the cut is marked.</summary>
    public int MaxBodyLength { get; set; } = 4096;

    /// <summary>The most bytes of a request or response body that are read to be logged; a bigger body is not logged.</summary>
    public int MaxBodyReadBytes { get; set; } = 256 * 1024;

    /// <summary>The longest value of a header written.</summary>
    public int MaxHeaderValueLength { get; set; } = 256;

    /// <summary>The header that carries the correlation id of a chain in and out. A request without one starts a new chain.</summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";
}
