using System.Security.Cryptography;
using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Storage.Files;

public interface IFileVolumeComparer
{
    bool AreOnSameVolume(string firstPath, string secondPath);
}

public sealed class PathRootFileVolumeComparer : IFileVolumeComparer
{
    public bool AreOnSameVolume(string firstPath, string secondPath) =>
        string.Equals(
            Path.GetPathRoot(firstPath),
            Path.GetPathRoot(secondPath),
            StringComparison.OrdinalIgnoreCase);
}

public sealed class AtomicTemporaryFileFinalizer : ITemporaryFileFinalizer
{
    private const int BufferSize = 128 * 1024;
    private readonly IFileVolumeComparer _volumeComparer;

    public AtomicTemporaryFileFinalizer()
        : this(new PathRootFileVolumeComparer())
    {
    }

    public AtomicTemporaryFileFinalizer(IFileVolumeComparer volumeComparer)
    {
        _volumeComparer = volumeComparer ?? throw new ArgumentNullException(nameof(volumeComparer));
    }

    public async ValueTask FinalizeAsync(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256,
        CancellationToken cancellationToken)
    {
        ValidateArguments(downloadId, temporaryPath, destinationPath, verifiedSha256);
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(destinationPath))
        {
            throw new IOException("The destination file already exists.");
        }

        await VerifyHashAsync(temporaryPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        if (_volumeComparer.AreOnSameVolume(temporaryPath, destinationPath))
        {
            File.Move(temporaryPath, destinationPath, overwrite: false);
            return;
        }

        await CopyAcrossVolumesAsync(
                downloadId,
                temporaryPath,
                destinationPath,
                verifiedSha256,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask RepairAsync(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256,
        CancellationToken cancellationToken)
    {
        ValidateArguments(downloadId, temporaryPath, destinationPath, verifiedSha256);
        cancellationToken.ThrowIfCancellationRequested();

        var temporaryExists = File.Exists(temporaryPath);
        var destinationExists = File.Exists(destinationPath);
        if (!temporaryExists && !destinationExists)
        {
            throw new InvalidDataException("Neither finalization file exists.");
        }

        var sameVolume = _volumeComparer.AreOnSameVolume(temporaryPath, destinationPath);
        if (temporaryExists && destinationExists)
        {
            if (sameVolume)
            {
                throw new InvalidDataException("Two files cannot result from an atomic same-volume move.");
            }

            await VerifyHashAsync(temporaryPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
            await VerifyHashAsync(destinationPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
            File.Delete(temporaryPath);
            DeleteOwnedStagingFileIfPresent(downloadId, destinationPath);
            return;
        }

        if (destinationExists)
        {
            await VerifyHashAsync(destinationPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
            DeleteOwnedStagingFileIfPresent(downloadId, destinationPath);
            return;
        }

        await VerifyHashAsync(temporaryPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        if (sameVolume)
        {
            File.Move(temporaryPath, destinationPath, overwrite: false);
            return;
        }

        await CopyAcrossVolumesAsync(
                downloadId,
                temporaryPath,
                destinationPath,
                verifiedSha256,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async ValueTask CopyAcrossVolumesAsync(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256,
        CancellationToken cancellationToken)
    {
        var stagingPath = GetStagingPath(downloadId, destinationPath);
        await PrepareStagingAsync(stagingPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        if (!File.Exists(stagingPath))
        {
            await using var source = new FileStream(
                temporaryPath,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    BufferSize = BufferSize,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                });
            await using var staging = new FileStream(
                stagingPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    BufferSize = BufferSize,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
                });

            await source.CopyToAsync(staging, BufferSize, cancellationToken).ConfigureAwait(false);
            await staging.FlushAsync(cancellationToken).ConfigureAwait(false);
            staging.Flush(flushToDisk: true);
        }

        await VerifyHashAsync(stagingPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        File.Move(stagingPath, destinationPath, overwrite: false);
        await VerifyHashAsync(destinationPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        File.Delete(temporaryPath);
    }

    private static async ValueTask PrepareStagingAsync(
        string stagingPath,
        string verifiedSha256,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(stagingPath))
        {
            return;
        }

        var attributes = File.GetAttributes(stagingPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("The reserved finalization staging path is a reparse point.");
        }

        try
        {
            await VerifyHashAsync(stagingPath, verifiedSha256, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            File.Delete(stagingPath);
        }
    }

    private static void DeleteOwnedStagingFileIfPresent(Guid downloadId, string destinationPath)
    {
        var stagingPath = GetStagingPath(downloadId, destinationPath);
        if (!File.Exists(stagingPath))
        {
            return;
        }

        var attributes = File.GetAttributes(stagingPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("The reserved finalization staging path is a reparse point.");
        }

        File.Delete(stagingPath);
    }

    private static async ValueTask VerifyHashAsync(
        string path,
        string verifiedSha256,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = BufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });
        var observed = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var expected = Convert.FromHexString(verifiedSha256);
        if (!CryptographicOperations.FixedTimeEquals(observed, expected))
        {
            throw new InvalidDataException("The finalization file SHA-256 does not match the persisted value.");
        }
    }

    private static string GetStagingPath(Guid downloadId, string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath) ??
            throw new ArgumentException("The destination path must have a parent directory.", nameof(destinationPath));
        return Path.Combine(directory, $".wdm-finalizing-{downloadId:N}.tmp");
    }

    private static void ValidateArguments(
        Guid downloadId,
        string temporaryPath,
        string destinationPath,
        string verifiedSha256)
    {
        if (downloadId == Guid.Empty)
        {
            throw new ArgumentException("The download identifier must not be empty.", nameof(downloadId));
        }

        ValidatePath(temporaryPath, nameof(temporaryPath));
        ValidatePath(destinationPath, nameof(destinationPath));
        if (string.Equals(temporaryPath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The temporary and destination paths must differ.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedSha256);
        if (verifiedSha256.Length != 64 || verifiedSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.", nameof(verifiedSha256));
        }
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
