using System.Security.Cryptography;
using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public sealed class Sha256TemporaryFileHasher : ITemporaryFileHasher
{
    public async ValueTask<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("The path must be absolute.", nameof(path));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }
}
