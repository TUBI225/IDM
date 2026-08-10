using System.Net;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Tests;

internal sealed class AllowAllNetworkAddressPolicy : INetworkAddressPolicy
{
    public void Validate(IReadOnlyList<IPAddress> addresses)
    {
        if (addresses.Count == 0)
        {
            throw new InvalidOperationException("The test resolver returned no address.");
        }
    }
}
