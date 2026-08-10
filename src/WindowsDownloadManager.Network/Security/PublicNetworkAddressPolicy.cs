using System.Net;
using System.Net.Sockets;

namespace WindowsDownloadManager.Network.Security;

public sealed class PublicNetworkAddressPolicy : INetworkAddressPolicy
{
    public void Validate(IReadOnlyList<IPAddress> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        if (addresses.Count == 0 || addresses.Any(IsNonPublic))
        {
            throw new UnsafeUriException(
                "The URI resolves to a local, private, reserved, or multicast address.");
        }
    }

    private static bool IsNonPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var isGlobalUnicast = (bytes[0] & 0xe0) == 0x20;
            var isDocumentation = bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
            return !isGlobalUnicast || isDocumentation || address.IsIPv6LinkLocal ||
                   address.IsIPv6Multicast || address.IsIPv6SiteLocal;
        }

        return bytes[0] switch
        {
            0 or 10 or 127 => true,
            100 when (bytes[1] & 0xc0) == 0x40 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] is >= 16 and <= 31 => true,
            192 when bytes[1] == 0 || bytes[1] == 168 => true,
            198 when bytes[1] is 18 or 19 || (bytes[1] == 51 && bytes[2] == 100) => true,
            203 when bytes[1] == 0 && bytes[2] == 113 => true,
            >= 224 => true,
            _ => false,
        };
    }
}
