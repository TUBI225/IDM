using System.Net;

namespace WindowsDownloadManager.Network.Http;

public interface IHostAddressResolver
{
    ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken);
}
