using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class StartupRecoveryReconciler(ITemporaryFileInspector temporaryFileInspector)
{
    public async ValueTask<TemporaryFileReconciliationResult> ReconcileAsync(
        DownloadTask task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.TemporaryPath is null)
        {
            return CreateResult(
                task,
                TemporaryFileReconciliationStatus.RecoveryMetadataAbsent,
                fileLength: null,
                safePosition: 0);
        }

        var snapshot = await temporaryFileInspector
            .InspectAsync(task.TemporaryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return CreateResult(
                task,
                TemporaryFileReconciliationStatus.TemporaryFileAbsent,
                fileLength: null,
                safePosition: 0);
        }

        var fileLength = snapshot.Length
            ?? throw new InvalidDataException("An existing temporary file must expose its length.");
        var status = fileLength.CompareTo(task.ConfirmedBytes) switch
        {
            < 0 => TemporaryFileReconciliationStatus.TemporaryFileShorter,
            0 => TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            > 0 => TemporaryFileReconciliationStatus.TemporaryFileLonger,
        };

        return CreateResult(
            task,
            status,
            fileLength,
            Math.Min(task.ConfirmedBytes, fileLength));
    }

    private static TemporaryFileReconciliationResult CreateResult(
        DownloadTask task,
        TemporaryFileReconciliationStatus status,
        long? fileLength,
        long safePosition) =>
        new(
            task.Id,
            status,
            task.TemporaryPath,
            task.ConfirmedBytes,
            fileLength,
            safePosition);
}
