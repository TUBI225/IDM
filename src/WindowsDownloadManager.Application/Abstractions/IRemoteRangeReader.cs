using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Abstractions;

public interface IRemoteRangeReader
{
    ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
        RemoteIdentity identity,
        long offset,
        int length,
        CancellationToken cancellationToken);
}
