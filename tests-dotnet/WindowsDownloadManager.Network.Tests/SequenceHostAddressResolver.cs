using System.Net;
using WindowsDownloadManager.Network.Http;

namespace WindowsDownloadManager.Network.Tests;

internal sealed class SequenceHostAddressResolver : IHostAddressResolver
{
    private readonly Queue<IReadOnlyList<IPAddress>> _results;

    public SequenceHostAddressResolver(params IReadOnlyList<IPAddress>[] results)
    {
        _results = new Queue<IReadOnlyList<IPAddress>>(results);
    }

    public int CallCount { get; private set; }

    public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        if (_results.Count == 0)
        {
            throw new InvalidOperationException("No scripted DNS result remains.");
        }

        return ValueTask.FromResult(_results.Dequeue());
    }
}
