using System;
using System.Net;
using System.Net.Sockets;

namespace HsnSoft.Base.Net;

/// <summary>Which addresses are the public internet and which are the inside of a network: what a service that calls an address from outside has to tell before it connects.</summary>
public static class PublicAddress
{
    /// <summary>Whether an address is one of the public internet: not private, loopback, link-local, multicast, unspecified or reserved (IPv4, IPv6 and IPv4 inside IPv6).</summary>
    public static bool IsPublic(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] six = address.GetAddressBytes();
            bool uniqueLocal = (six[0] & 0xFE) == 0xFC;   // fc00::/7
            bool multicast = six[0] == 0xFF;
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || uniqueLocal || multicast);
        }

        byte[] four = address.GetAddressBytes();
        return !(four[0] == 0                                                       // 0.0.0.0/8
            || four[0] == 10                                                        // 10.0.0.0/8
            || (four[0] == 100 && four[1] is >= 64 and <= 127)                      // 100.64.0.0/10 (shared address space)
            || four[0] == 127                                                       // 127.0.0.0/8
            || (four[0] == 169 && four[1] == 254)                                   // 169.254.0.0/16 (link-local, the cloud metadata address)
            || (four[0] == 172 && four[1] is >= 16 and <= 31)                       // 172.16.0.0/12
            || (four[0] == 192 && four[1] == 0 && four[2] is 0 or 2)                // 192.0.0.0/24 and 192.0.2.0/24
            || (four[0] == 192 && four[1] == 168)                                   // 192.168.0.0/16
            || (four[0] == 198 && four[1] is 18 or 19)                              // 198.18.0.0/15 (benchmarking)
            || (four[0] == 198 && four[1] == 51 && four[2] == 100)                  // 198.51.100.0/24
            || (four[0] == 203 && four[1] == 0 && four[2] == 113)                   // 203.0.113.0/24
            || four[0] >= 224);                                                     // multicast and reserved
    }
}

/// <summary>A host name that is allowed: exactly (<c>files.example.com</c>), or every name under a domain (<c>*.example.com</c>, not the domain itself).</summary>
public static class HostPattern
{
    /// <summary>Whether <paramref name="host"/> matches <paramref name="pattern"/>; names are not case sensitive.</summary>
    public static bool Matches(string host, string pattern)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(pattern);

        return pattern.StartsWith("*.", StringComparison.Ordinal)
            ? host.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase) && host.Length > pattern.Length - 1
            : string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
