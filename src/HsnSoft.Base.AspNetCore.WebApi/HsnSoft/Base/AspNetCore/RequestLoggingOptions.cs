using System.Collections.Generic;

namespace HsnSoft.Base.AspNetCore;

/// <summary>What the request logging middleware writes (section <c>RequestLogging</c>).</summary>
public sealed class RequestLoggingOptions
{
    public const string SectionName = "RequestLogging";

    /// <summary>Turns the request and response logs off (the correlation id is still set).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Health probes are called every few seconds; their logs are off unless this is on.</summary>
    public bool LogHealthChecks { get; set; }

    /// <summary>Paths (prefixes) that are never logged, e.g. <c>/openapi</c>.</summary>
    public List<string> ExcludedPaths { get; set; } = ["/openapi", "/swagger"];

    /// <summary>
    /// Writes the JSON bodies of requests and responses. Off by default; a body is only ever written masked, so it also needs an
    /// <c>ILogMasker</c> (<c>AddBaseLogging()</c>): without one no body is logged.
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
