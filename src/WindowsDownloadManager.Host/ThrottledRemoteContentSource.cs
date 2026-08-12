using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Décorateur de production qui applique un contrôle de débit sur chaque lecture du flux distant.
/// Le delegate d'acquisition est construit par le `DownloadHost` autour du `BandwidthController`
/// (une acquisition par bloc de lecture, bornée par la taille demandée). Les lectures synchrones et
/// les écritures restent déléguées sans acquisition.
/// </summary>
public sealed class ThrottledRemoteContentSource : IRemoteContentSource
{
    private readonly IRemoteContentSource _inner;
    private readonly Func<int, CancellationToken, ValueTask> _acquire;

    public ThrottledRemoteContentSource(
        IRemoteContentSource inner,
        Func<int, CancellationToken, ValueTask> acquire)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _acquire = acquire ?? throw new ArgumentNullException(nameof(acquire));
    }

    public async ValueTask<RemoteContentLease> OpenReadAsync(
        RemoteResourceInfo resource,
        long offset,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var lease = await _inner
            .OpenReadAsync(resource, offset, cancellationToken)
            .ConfigureAwait(false);
        return new RemoteContentLease(
            new ThrottledStream(lease.Content, _acquire),
            lease.TotalLength);
    }

    private sealed class ThrottledStream(Stream inner, Func<int, CancellationToken, ValueTask> acquire) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await acquire(buffer.Length, cancellationToken).ConfigureAwait(false);
            return await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken)
        {
            await acquire(buffer.Length, cancellationToken).ConfigureAwait(false);
            await inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync().ConfigureAwait(false);
            base.Dispose();
        }
    }
}
