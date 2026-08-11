using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class DownloadFinalizationCoordinator
{
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
        await FinalizeAsync(task, expectedSha256: null, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FinalizeAsync(
        DownloadTask task,
        string? expectedSha256,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.State != DownloadState.Verifying)
        {
            throw new InvalidOperationException("Only a verified download can be finalized.");
        }

        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var temporaryPath = RequireTemporaryPath(task);
            await VerifyExpectedFileAsync(task, temporaryPath, cancellationToken).ConfigureAwait(false);
            var destination = await _fileInspector
                .InspectAsync(task.DestinationPath, cancellationToken)
                .ConfigureAwait(false);
            if (destination.Exists)
            {
                throw new IOException("The destination file already exists.");
            }

            var verifiedSha256 = await _fileHasher
                .ComputeSha256Async(temporaryPath, cancellationToken)
                .ConfigureAwait(false);
            if (expectedSha256 is not null &&
                !HashesMatch(verifiedSha256, expectedSha256))
            {
                throw new InvalidDataException("The temporary file SHA-256 does not match the expected value.");
            }

            task.RecordVerifiedSha256(verifiedSha256);
            task.TransitionTo(DownloadState.Finalizing);
            await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            await _fileFinalizer
                .MoveAtomicallyAsync(temporaryPath, task.DestinationPath, cancellationToken)
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

            if (temporary.Exists == destination.Exists)
            {
                throw new InvalidDataException(
                    "Finalization repair requires exactly one of the temporary and destination files to exist.");
            }

            if (temporary.Exists)
            {
                ValidateLength(task, temporary);
                await VerifyPersistedHashAsync(task, temporaryPath, cancellationToken).ConfigureAwait(false);
                await _fileFinalizer
                    .MoveAtomicallyAsync(temporaryPath, task.DestinationPath, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                ValidateLength(task, destination);
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
