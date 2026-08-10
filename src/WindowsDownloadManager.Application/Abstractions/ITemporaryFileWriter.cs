namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileWriter
{
    ValueTask PrepareNewAsync(
        string temporaryPath,
        CancellationToken cancellationToken);

    ValueTask<long> WriteAndFlushAsync(
        string temporaryPath,
        long offset,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);
}
