namespace WindowsDownloadManager.Application.Downloads;

public enum RecoveryDecisionStatus
{
    Blocked,
    ReadyForOverlapVerification,
}

[Flags]
public enum RecoveryBlocker
{
    None = 0,
    RecoveryMetadataAbsent = 1 << 0,
    TemporaryFileAbsent = 1 << 1,
    CheckpointAheadOfTemporaryFile = 1 << 2,
    UnconfirmedTemporaryFileTail = 1 << 3,
    RemoteIdentityContradictory = 1 << 4,
    RemoteIdentityEvidenceInsufficient = 1 << 5,
    ByteRangeResumeUnavailable = 1 << 6,
}

public sealed record RecoveryDecisionResult(
    Guid DownloadId,
    RecoveryDecisionStatus Status,
    RecoveryBlocker Blockers,
    long SafePosition,
    TemporaryFileReconciliationResult TemporaryFile,
    RemoteIdentityReconciliationResult RemoteIdentity);
