using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WindowsDownloadManager.Network.Tests;

internal sealed class LoopbackHttpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _serverTask;
    private readonly string _response;
    private readonly TimeSpan _responseDelay;

    public LoopbackHttpServer(string response, TimeSpan? responseDelay = null)
    {
        _response = response;
        _responseDelay = responseDelay ?? TimeSpan.Zero;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Uri = new Uri($"http://127.0.0.1:{port}/file.bin");
        _serverTask = ServeOnceAsync();
    }

    public Uri Uri { get; }
    public string? RequestText { get; private set; }

    public Uri CreateUri(string host) => new UriBuilder(Uri) { Host = host }.Uri;

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        await _serverTask.ConfigureAwait(false);
    }

    private async Task ServeOnceAsync()
    {
        try
        {
            using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var stream = client.GetStream();
            using var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var request = new StringBuilder();
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                request.AppendLine(line);
                if (line.Length == 0)
                {
                    break;
                }
            }

            RequestText = request.ToString();
            if (_responseDelay > TimeSpan.Zero)
            {
                await Task.Delay(_responseDelay).ConfigureAwait(false);
            }

            var bytes = Encoding.ASCII.GetBytes(_response);
            await stream.WriteAsync(bytes).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
    }
}
