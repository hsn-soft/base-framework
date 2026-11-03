using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Net;

namespace HsnSoft.Base.Http;

/// <summary>
/// What a client may call when the address comes from outside (a file or a page a user pointed to): a guard against the server being made to call the inside of its own network (the cloud metadata address, a database, an admin port,
/// another service). The address is checked where the connection is made (<see cref="SsrfGuard.ConnectAsync"/>), after the name was resolved, so a name that resolves to an inside address (or changes its answer between two lookups) is refused too.
/// </summary>
public sealed class SsrfGuardOptions
{
    /// <summary>The ports that may be called (default 80 and 443); empty for any.</summary>
    public IList<int> AllowedPorts { get; } = [80, 443];

    /// <summary>The hosts that may be called, a name (<c>files.example.com</c>) or <c>*.example.com</c> for the names under it; empty for any public host.</summary>
    public IList<string> AllowedHosts { get; } = [];

    /// <summary>Whether only https may be called (default <see langword="false"/>).</summary>
    public bool RequireHttps { get; set; }

    /// <summary>Whether an address of the inside of a network (private, loopback, link-local, ...) may be called (default <see langword="false"/>). For a development machine, never for a service that fetches what users point to.</summary>
    public bool AllowPrivateNetworks { get; set; }
}

/// <summary>The call was refused by the guard, with the reason.</summary>
public sealed class SsrfBlockedException(string message) : HttpRequestException(message);

/// <summary>Checks the scheme, the host and the port of a call before it is sent.</summary>
public sealed class SsrfGuardHandler(SsrfGuardOptions options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        SsrfGuard.CheckAddress(request.RequestUri, options);
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>The checks of the guard, and the connection that makes the last one.</summary>
public static class SsrfGuard
{
    /// <summary>The scheme, the port and the host of an address; an <see cref="SsrfBlockedException"/> when one is not allowed (an address that is an IP is checked here too).</summary>
    public static void CheckAddress(Uri? uri, SsrfGuardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (uri is null || !uri.IsAbsoluteUri)
        {
            throw new SsrfBlockedException("The address must be absolute.");
        }

        if (uri.Scheme is not ("http" or "https") || (options.RequireHttps && uri.Scheme != "https"))
        {
            throw new SsrfBlockedException($"The scheme '{uri.Scheme}' is not allowed.");
        }

        if (options.AllowedPorts.Count > 0 && !options.AllowedPorts.Contains(uri.Port))
        {
            throw new SsrfBlockedException($"The port {uri.Port} is not allowed.");
        }

        if (options.AllowedHosts.Count > 0 && !options.AllowedHosts.Any(allowed => HostPattern.Matches(uri.Host, allowed)))
        {
            throw new SsrfBlockedException($"The host '{uri.Host}' is not allowed.");
        }

        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out IPAddress? literal) && !options.AllowPrivateNetworks && !IsPublic(literal))
        {
            throw new SsrfBlockedException("The address is of the inside of a network.");
        }
    }

    /// <summary>Whether an address is one of the public internet: see <see cref="PublicAddress.IsPublic"/>.</summary>
    public static bool IsPublic(IPAddress address) => PublicAddress.IsPublic(address);

    /// <summary>
    /// The connection of <see cref="SocketsHttpHandler.ConnectCallback"/> for a guarded client: the name is resolved here and only a public address (unless private networks are allowed) is connected to, so the address that was checked is the
    /// address that is called.
    /// </summary>
    public static async ValueTask<System.IO.Stream> ConnectAsync(SsrfGuardOptions options, SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(context);

        IPAddress[] addresses = IPAddress.TryParse(context.DnsEndPoint.Host, out IPAddress? literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
        IPAddress[] allowed = [.. addresses.Where(address => options.AllowPrivateNetworks || IsPublic(address))];
        if (allowed.Length == 0)
        {
            throw new SsrfBlockedException($"'{context.DnsEndPoint.Host}' resolves to the inside of a network or to nothing.");
        }

        Exception? last = null;
        foreach (IPAddress address in allowed)
        {
            Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception) when (exception is SocketException)
            {
                socket.Dispose();
                last = exception;
            }
        }

        throw new HttpRequestException($"'{context.DnsEndPoint.Host}' could not be reached.", last);
    }
}
