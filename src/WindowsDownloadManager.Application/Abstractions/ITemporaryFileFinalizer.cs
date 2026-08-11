namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileFinalizer
{
    ValueTask FinalizeAsync(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256,
        CancellationToken cancellationToken);

    ValueTask RepairAsync(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256,
        CancellationToken cancellationToken);
}
