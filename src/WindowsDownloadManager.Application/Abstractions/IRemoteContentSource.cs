namespace WindowsDownloadManager.Application.Abstractions;

public interface IRemoteContentSource
{
    ValueTask<RemoteContentLease> OpenReadAsync(
        RemoteResourceInfo resource,
        long offset,
        CancellationToken cancellationToken);
}

public sealed class RemoteContentLease : IAsyncDisposable
{
    private readonly IDisposable? _owner;

    public RemoteContentLease(Stream content, long? totalLength, IDisposable? owner = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        TotalLength = totalLength;
        _owner = owner;
    }

    public Stream Content { get; }
    public long? TotalLength { get; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Content.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _owner?.Dispose();
        }
    }
}
