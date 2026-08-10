using System.Net;
using System.Net.Sockets;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Http;

public sealed class DnsHostAddressResolver : IHostAddressResolver
{
    public async ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        try
        {
            return await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException exception)
        {
            throw new UnsafeUriException(
                $"The remote host could not be resolved: {exception.SocketErrorCode}.");
        }
    }
}
