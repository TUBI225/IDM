namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileRangeReader
{
    ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
        string temporaryPath,
        long offset,
        int length,
        CancellationToken cancellationToken);
}

public sealed record TemporaryFileRangeSnapshot(
    long FileLength,
    ReadOnlyMemory<byte> Content);
