using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class DownloadFinalizationCoordinator
{
    private const int MaximumCollisionCandidates = 10_000;
    private readonly ITemporaryFileInspector _fileInspector;
    private readonly ITemporaryFileHasher _fileHasher;
    private readonly ITemporaryFileFinalizer _fileFinalizer;
    private readonly IDownloadRepository _downloadRepository;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public DownloadFinalizationCoordinator(
        ITemporaryFileInspector fileInspector,
        ITemporaryFileHasher fileHasher,
        ITemporaryFileFinalizer fileFinalizer,
        IDownloadRepository downloadRepository)
    {
        _fileInspector = fileInspector ?? throw new ArgumentNullException(nameof(fileInspector));
        _fileHasher = fileHasher ?? throw new ArgumentNullException(nameof(fileHasher));
        _fileFinalizer = fileFinalizer ?? throw new ArgumentNullException(nameof(fileFinalizer));
        _downloadRepository = downloadRepository ?? throw new ArgumentNullException(nameof(downloadRepository));
    }

    public async ValueTask FinalizeAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        await FinalizeAsync(task, expectedSha256: null, DestinationCollisionPolicy.Fail, allowForcedBypass: false, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FinalizeAsync(
        DownloadTask task,
        string? expectedSha256,
        CancellationToken cancellationToken)
    {
        await FinalizeAsync(
                task,
                expectedSha256,
                DestinationCollisionPolicy.Fail,
                allowForcedBypass: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask FinalizeAsync(
        DownloadTask task,
        string? expectedSha256,
        DestinationCollisionPolicy collisionPolicy,
        CancellationToken cancellationToken)
    {
        await FinalizeAsync(
                task,
                expectedSha256,
                collisionPolicy,
                allowForcedBypass: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask FinalizeAsync(
        DownloadTask task,
        string? expectedSha256,
        DestinationCollisionPolicy collisionPolicy,
        bool allowForcedBypass,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (!Enum.IsDefined(collisionPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(collisionPolicy));
        }

        if (task.State != DownloadState.Verifying)
        {
            throw new InvalidOperationException("Only a verified download can be finalized.");
        }

        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var temporaryPath = RequireTemporaryPath(task);
            await VerifyExpectedFileAsync(task, temporaryPath, cancellationToken).ConfigureAwait(false);
            var resolvedDestination = await ResolveDestinationAsync(
                    task.DestinationPath,
                    collisionPolicy,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(resolvedDestination, task.DestinationPath, StringComparison.OrdinalIgnoreCase))
            {
                task.ResolveDestinationCollision(resolvedDestination);
            }

            var verifiedSha256 = await _fileHasher
                .ComputeSha256Async(temporaryPath, cancellationToken)
                .ConfigureAwait(false);
            var effectiveExpectedSha256 = expectedSha256 ?? task.RemoteIdentity?.Sha256;
            if (effectiveExpectedSha256 is not null &&
                !HashesMatch(verifiedSha256, effectiveExpectedSha256))
            {
                if (!allowForcedBypass)
                {
                    throw new InvalidDataException("The temporary file SHA-256 does not match the expected value.");
                }
            }

            task.RecordVerifiedSha256(verifiedSha256);
            task.TransitionTo(DownloadState.Finalizing);
            await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            await _fileFinalizer
                .FinalizeAsync(
                    task.Id,
                    temporaryPath,
                    task.DestinationPath,
                    verifiedSha256,
                    cancellationToken)
                .ConfigureAwait(false);
            await VerifyPersistedHashAsync(task, task.DestinationPath, cancellationToken).ConfigureAwait(false);
            task.TransitionTo(DownloadState.Completed);
            await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async ValueTask RepairAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.State != DownloadState.Finalizing)
        {
            throw new InvalidOperationException("Only a persisted Finalizing task can be repaired.");
        }

        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var temporaryPath = RequireTemporaryPath(task);
            var temporary = await _fileInspector.InspectAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
            var destination = await _fileInspector.InspectAsync(task.DestinationPath, cancellationToken).ConfigureAwait(false);

            if (!temporary.Exists && !destination.Exists)
            {
                throw new InvalidDataException(
                    "Finalization repair requires the temporary or destination file to exist.");
            }

            if (temporary.Exists)
            {
                ValidateLength(task, temporary);
                await VerifyPersistedHashAsync(task, temporaryPath, cancellationToken).ConfigureAwait(false);
            }

            if (destination.Exists)
            {
                ValidateLength(task, destination);
                await VerifyPersistedHashAsync(task, task.DestinationPath, cancellationToken).ConfigureAwait(false);
            }

            if (temporary.Exists)
            {
                await _fileFinalizer
                    .RepairAsync(
                        task.Id,
                        temporaryPath,
                        task.DestinationPath,
                        task.VerifiedSha256!,
                        cancellationToken)
                    .ConfigureAwait(false);
                await VerifyPersistedHashAsync(task, task.DestinationPath, cancellationToken).ConfigureAwait(false);
            }

            task.TransitionTo(DownloadState.Completed);
            await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private async ValueTask VerifyExpectedFileAsync(
        DownloadTask task,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        var temporary = await _fileInspector
            .InspectAsync(temporaryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!temporary.Exists)
        {
            throw new FileNotFoundException("The temporary file is missing.", temporaryPath);
        }

        ValidateLength(task, temporary);
    }

    private async ValueTask<string> ResolveDestinationAsync(
        string destinationPath,
        DestinationCollisionPolicy collisionPolicy,
        CancellationToken cancellationToken)
    {
        var destination = await _fileInspector
            .InspectAsync(destinationPath, cancellationToken)
            .ConfigureAwait(false);
        if (!destination.Exists)
        {
            return destinationPath;
        }

        if (collisionPolicy == DestinationCollisionPolicy.Fail)
        {
            throw new IOException("The destination file already exists.");
        }

        var directory = Path.GetDirectoryName(destinationPath) ??
            throw new InvalidDataException("The destination path has no parent directory.");
        var extension = Path.GetExtension(destinationPath);
        var fileName = Path.GetFileNameWithoutExtension(destinationPath);
        for (var suffix = 1; suffix <= MaximumCollisionCandidates; suffix++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = Path.Combine(directory, $"{fileName} ({suffix}){extension}");
            var snapshot = await _fileInspector
                .InspectAsync(candidate, cancellationToken)
                .ConfigureAwait(false);
            if (!snapshot.Exists)
            {
                return candidate;
            }
        }

        throw new IOException("No collision-free destination name could be reserved.");
    }

    private static void ValidateLength(DownloadTask task, TemporaryFileSnapshot snapshot)
    {
        var actualLength = snapshot.Length ??
            throw new InvalidDataException("An existing file must expose its length.");
        var expectedLength = task.RemoteIdentity?.Length ?? task.ConfirmedBytes;
        if (actualLength != task.ConfirmedBytes || actualLength != expectedLength)
        {
            throw new InvalidDataException("The file length does not match the confirmed remote length.");
        }
    }

    private async ValueTask VerifyPersistedHashAsync(
        DownloadTask task,
        string path,
        CancellationToken cancellationToken)
    {
        var expectedSha256 = task.VerifiedSha256 ??
            throw new InvalidDataException("The finalizing task has no persisted SHA-256 verification.");
        var observedSha256 = await _fileHasher
            .ComputeSha256Async(path, cancellationToken)
            .ConfigureAwait(false);
        if (!HashesMatch(observedSha256, expectedSha256))
        {
            throw new InvalidDataException("The finalization file SHA-256 no longer matches the persisted value.");
        }
    }

    private static bool HashesMatch(string first, string second)
    {
        try
        {
            var firstBytes = Convert.FromHexString(first);
            var secondBytes = Convert.FromHexString(second);
            return firstBytes.Length == 32 &&
                secondBytes.Length == 32 &&
                System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(firstBytes, secondBytes);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.", exception);
        }
    }

    private static string RequireTemporaryPath(DownloadTask task) =>
        task.TemporaryPath ?? throw new InvalidDataException("The task has no temporary path.");
}
