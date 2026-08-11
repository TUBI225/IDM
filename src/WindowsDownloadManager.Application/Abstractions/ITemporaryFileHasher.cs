namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileHasher
{
    ValueTask<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken);
}
