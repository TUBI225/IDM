namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileFinalizer
{
    ValueTask MoveAtomicallyAsync(
        string temporaryPath,
        string destinationPath,
        CancellationToken cancellationToken);
}
