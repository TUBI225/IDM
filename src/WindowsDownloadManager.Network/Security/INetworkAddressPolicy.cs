using System.Net;

namespace WindowsDownloadManager.Network.Security;

public interface INetworkAddressPolicy
{
    void Validate(IReadOnlyList<IPAddress> addresses);
}
