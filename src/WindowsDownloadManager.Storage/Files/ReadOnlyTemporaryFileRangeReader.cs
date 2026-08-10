using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public sealed class ReadOnlyTemporaryFileRangeReader : ITemporaryFileRangeReader
{
    public async ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
        string temporaryPath,
        long offset,
        int length,
        CancellationToken cancellationToken)
    {
        ValidateArguments(temporaryPath, offset, length);
        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            temporaryPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.RandomAccess);
        var fileLength = stream.Length;
        if (checked(offset + length) > fileLength)
        {
            return new TemporaryFileRangeSnapshot(fileLength, ReadOnlyMemory<byte>.Empty);
        }

        stream.Position = offset;
        var content = new byte[length];
        await stream.ReadExactlyAsync(content, cancellationToken).ConfigureAwait(false);
        return new TemporaryFileRangeSnapshot(fileLength, content);
    }

    private static void ValidateArguments(string temporaryPath, long offset, int length)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        if (!Path.IsPathFullyQualified(temporaryPath))
        {
            throw new ArgumentException("The temporary path must be absolute.", nameof(temporaryPath));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
    }
}
