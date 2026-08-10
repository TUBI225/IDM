using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public sealed class AtomicTemporaryFileFinalizer : ITemporaryFileFinalizer
{
    public ValueTask MoveAtomicallyAsync(
        string temporaryPath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ValidatePath(temporaryPath, nameof(temporaryPath));
        ValidatePath(destinationPath, nameof(destinationPath));
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(temporaryPath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The temporary and destination paths must differ.");
        }

        if (!string.Equals(
                Path.GetPathRoot(temporaryPath),
                Path.GetPathRoot(destinationPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Atomic finalization requires the temporary and destination files to share a volume.");
        }

        File.Move(temporaryPath, destinationPath, overwrite: false);
        return ValueTask.CompletedTask;
    }

    private static void ValidatePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("The path must be absolute.", parameterName);
        }
    }
}
