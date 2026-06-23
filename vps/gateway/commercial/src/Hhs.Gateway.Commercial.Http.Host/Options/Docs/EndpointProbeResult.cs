using System.Net;

namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class EndpointProbeResult
{
    public string Url { get; set; } = string.Empty;

    public HttpStatusCode? StatusCode { get; set; }

    public bool IsSuccess { get; set; }

    public long ResponseTimeMs { get; set; }

    public string? ResponseSnippet { get; set; }

    public string? ErrorMessage { get; set; }
}