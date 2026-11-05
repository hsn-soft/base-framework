using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Http;

/// <summary>
/// Sends the bearer token of the user being served (a web host that keeps it in the cookie, a service that got it in the request) with the call, and only to the hosts it is for: a token sent to any host a request names would be
/// handed to whoever controls it. A call to another host is sent without it; a call that already has an <c>Authorization</c> header keeps its own.
/// </summary>
public sealed class BearerTokenRelayHandler : DelegatingHandler
{
    private readonly Func<string?> _token;
    private readonly HashSet<string> _hosts;

    /// <param name="token">Gives the token of the user being served, or <see langword="null"/> or empty when there is none.</param>
    /// <param name="allowedHosts">The hosts that may get the token (<c>gateway.internal</c>); at least one.</param>
    public BearerTokenRelayHandler(Func<string?> token, IEnumerable<string> allowedHosts)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(allowedHosts);

        _token = token;
        _hosts = new HashSet<string>(allowedHosts.Where(host => !string.IsNullOrWhiteSpace(host)).Select(host => host.Trim()), StringComparer.OrdinalIgnoreCase);
        if (_hosts.Count == 0)
        {
            throw new ArgumentException("The hosts that may get the token are required.", nameof(allowedHosts));
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null && request.RequestUri is { IsAbsoluteUri: true } uri && _hosts.Contains(uri.Host) && _token() is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
