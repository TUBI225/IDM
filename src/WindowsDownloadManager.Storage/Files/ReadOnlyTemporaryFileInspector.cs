using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public sealed class ReadOnlyTemporaryFileInspector : ITemporaryFileInspector
{
    public ValueTask<TemporaryFileSnapshot> InspectAsync(
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        ValidatePath(temporaryPath);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var stream = new FileStream(
                temporaryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 1,
                FileOptions.RandomAccess);
            return ValueTask.FromResult(TemporaryFileSnapshot.Existing(stream.Length));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return ValueTask.FromResult(TemporaryFileSnapshot.Absent);
        }
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
