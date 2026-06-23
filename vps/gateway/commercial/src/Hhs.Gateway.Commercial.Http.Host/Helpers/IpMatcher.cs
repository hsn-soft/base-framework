using System.Net;
using System.Net.Sockets;

namespace Hhs.Gateway.Commercial.Helpers;

public static class IpMatcher
{
    public static bool IsMatch(IPAddress? address, IEnumerable<string> cidrsOrIps)
    {
        if (address is null)
            return false;

        foreach (var item in cidrsOrIps)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            if (item.Contains('/'))
            {
                if (IsInCidr(address, item))
                    return true;
            }
            else if (IPAddress.TryParse(item, out var ip) && address.Equals(ip))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInCidr(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        if (!IPAddress.TryParse(parts[0], out var network))
            return false;

        if (!int.TryParse(parts[1], out var prefixLength))
            return false;

        var addrBytes = Normalize(address);
        var networkBytes = Normalize(network);

        if (addrBytes.Length != networkBytes.Length)
            return false;

        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (addrBytes[i] != networkBytes[i])
                return false;
        }

        if (remainingBits == 0)
            return true;

        byte mask = (byte)~(255 >> remainingBits);
        return (addrBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }

    private static byte[] Normalize(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.GetAddressBytes();
    }
}