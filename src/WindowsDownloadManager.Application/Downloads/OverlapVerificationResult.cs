namespace WindowsDownloadManager.Application.Downloads;

public enum OverlapVerificationStatus
{
    NotRequired,
    Match,
    Mismatch,
    LocalFileChanged,
}

public sealed record OverlapVerificationResult(
    Guid DownloadId,
    OverlapVerificationStatus Status,
    long Offset,
    int Length,
    long SafePosition,
    long? ObservedFileLength);
