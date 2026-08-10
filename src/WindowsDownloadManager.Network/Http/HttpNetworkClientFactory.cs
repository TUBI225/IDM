using System.Net;
using System.Net.Sockets;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Http;

public static class HttpNetworkClientFactory
{
    public static HttpClient Create(
        IHostAddressResolver addressResolver,
        INetworkAddressPolicy addressPolicy)
    {
        return new HttpClient(CreateHandler(addressResolver, addressPolicy), disposeHandler: true);
    }

    public static SocketsHttpHandler CreateHandler(
        IHostAddressResolver addressResolver,
        INetworkAddressPolicy addressPolicy)
    {
        ArgumentNullException.ThrowIfNull(addressResolver);
        ArgumentNullException.ThrowIfNull(addressPolicy);

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            UseProxy = false,
            ConnectCallback = (context, cancellationToken) => ConnectAsync(
                context.DnsEndPoint,
                addressResolver,
                addressPolicy,
                cancellationToken),
        };
    }

    private static async ValueTask<Stream> ConnectAsync(
        DnsEndPoint endpoint,
        IHostAddressResolver addressResolver,
        INetworkAddressPolicy addressPolicy,
        CancellationToken cancellationToken)
    {
        var addresses = await addressResolver.ResolveAsync(endpoint.Host, cancellationToken)
            .ConfigureAwait(false);
        addressPolicy.Validate(addresses);

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), cancellationToken)
                    .ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                throw;
            }
            catch (Exception exception) when (exception is SocketException or IOException)
            {
                socket.Dispose();
                lastError = exception;
            }
        }

        throw new HttpRequestException("No validated remote address accepted the connection.", lastError);
    }
}
