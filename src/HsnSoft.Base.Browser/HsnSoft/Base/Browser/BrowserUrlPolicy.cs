using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Net;

namespace HsnSoft.Base.Browser;

/// <summary>What a rendered page may load (section <c>Browser</c> of the provider): a page that is rendered runs scripts and fetches what it names, so an address that came from outside must not be able to make the browser call the inside of the network.</summary>
public sealed class BrowserUrlPolicyOptions
{
    /// <summary>The hosts a page may load from: a name, or <c>*.example.com</c>; empty for any public host.</summary>
    public IList<string> AllowedHosts { get; } = [];

    /// <summary>Whether addresses of the inside of a network (private, loopback, link-local, ...) may be loaded. Default: <see langword="false"/>; for a development machine only.</summary>
    public bool AllowPrivateNetworks { get; set; }
}

/// <summary>
/// Decides, for every address a rendered page asks for (the page, its redirects, its images, scripts and calls), whether the browser may load it: <c>http</c> and <c>https</c> to a host that is allowed and whose addresses are all public (the name is
/// resolved here), and <c>data:</c> and <c>about:blank</c>, which load nothing. <c>file:</c>, <c>ftp:</c> and the rest are refused. A browser resolves a name again when it connects, so a name that changes its answer between the two lookups is not stopped here:
/// where that matters the network itself must not let the browser reach the inside.
/// </summary>
public sealed class BrowserUrlPolicy(BrowserUrlPolicyOptions options)
{
    // what the lookup of a host said, for a minute (a page asks for the same host many times); never more than a thousand hosts
    private static readonly TimeSpan s_remember = TimeSpan.FromSeconds(60);
    private const int MaxHosts = 1000;

    private readonly Dictionary<string, (bool Allowed, DateTimeOffset Until)> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    /// <summary>Whether the browser may load <paramref name="url"/>.</summary>
    public async Task<bool> AllowsAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (url.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (options.AllowedHosts.Count > 0 && !options.AllowedHosts.Any(allowed => HostPattern.Matches(uri.Host, allowed)))
        {
            return false;
        }

        if (options.AllowPrivateNetworks)
        {
            return true;
        }

        string host = uri.Host.Trim('[', ']');
        lock (_gate)
        {
            if (_hosts.TryGetValue(host, out (bool Allowed, DateTimeOffset Until) known) && known.Until > DateTimeOffset.UtcNow)
            {
                return known.Allowed;
            }
        }

        bool allowed = await ArePublicAsync(host, cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            if (_hosts.Count >= MaxHosts)
            {
                _hosts.Clear();
            }

            _hosts[host] = (allowed, DateTimeOffset.UtcNow + s_remember);
        }

        return allowed;
    }

    private static async Task<bool> ArePublicAsync(string host, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out IPAddress? literal))
        {
            return PublicAddress.IsPublic(literal);
        }

        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
            return addresses.Length > 0 && addresses.All(PublicAddress.IsPublic);
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
    }
}
