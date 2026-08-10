using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public sealed class DurableTemporaryFileWriter : ITemporaryFileWriter
{
    public async ValueTask PrepareNewAsync(
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        ValidatePath(temporaryPath);
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 1,
            FileOptions.Asynchronous);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    public async ValueTask<long> WriteAndFlushAsync(
        string temporaryPath,
        long offset,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        ValidatePath(temporaryPath);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(
            temporaryPath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.RandomAccess);
        stream.Position = offset;
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
        return checked(offset + content.Length);
    }

    private static void ValidatePath(string temporaryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        if (!Path.IsPathFullyQualified(temporaryPath))
        {
            throw new ArgumentException("The temporary path must be absolute.", nameof(temporaryPath));
        }
    }
}
