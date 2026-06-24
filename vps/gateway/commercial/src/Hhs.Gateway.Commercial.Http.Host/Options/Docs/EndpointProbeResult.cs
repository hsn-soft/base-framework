using System.Net;
using JetBrains.Annotations;

namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class EndpointProbeResult
{
    public string Url { get; set; } = string.Empty;

    public HttpStatusCode? StatusCode { get; set; }

    public bool IsSuccess { get; set; }

    public long ResponseTimeMs { get; set; }

    [CanBeNull] public string ResponseSnippet { get; set; }

    [CanBeNull] public string ErrorMessage { get; set; }
}