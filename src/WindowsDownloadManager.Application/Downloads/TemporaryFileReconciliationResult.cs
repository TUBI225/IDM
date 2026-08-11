namespace WindowsDownloadManager.Application.Downloads;

public enum TemporaryFileReconciliationStatus
{
    RecoveryMetadataAbsent,
    TemporaryFileAbsent,
    TemporaryFileShorter,
    TemporaryFileMatchesCheckpoint,
    TemporaryFileLonger,
}

public sealed record TemporaryFileReconciliationResult(
    Guid DownloadId,
    TemporaryFileReconciliationStatus Status,
    string? TemporaryPath,
    long ConfirmedBytes,
    long? FileLength,
    long SafePosition);
