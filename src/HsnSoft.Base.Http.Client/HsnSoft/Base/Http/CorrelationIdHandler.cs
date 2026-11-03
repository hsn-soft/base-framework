using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;

namespace HsnSoft.Base.Http;

/// <summary>
/// Puts the correlation id of the chain the code is part of (<see cref="LogFlow.Current"/>: the request being served, the message being handled) on the call, so the page, the gateway and the services behind it are one chain in the logs.
/// A call that already has the header keeps it; outside any chain nothing is added.
/// </summary>
public sealed class CorrelationIdHandler(string headerName) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (LogFlow.Current?.CorrelationId is { Length: > 0 } correlationId && !request.Headers.Contains(headerName))
        {
            request.Headers.TryAddWithoutValidation(headerName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
