namespace WindowsDownloadManager.Application.Downloads;

public enum StartupRecoveryAssessmentStatus
{
    BlockedBeforeRemoteAnalysis,
    BlockedAfterRemoteAnalysis,
    OverlapNotRequired,
    OverlapMatched,
    OverlapMismatched,
    LocalFileChangedDuringOverlap,
}

public sealed record StartupRecoveryAssessmentResult(
    Guid DownloadId,
    StartupRecoveryAssessmentStatus Status,
    RecoveryBlocker ReconciliationBlockers,
    TemporaryFileReconciliationResult TemporaryFile,
    RemoteIdentityReconciliationResult? RemoteIdentity,
    RecoveryDecisionResult? Decision,
    OverlapVerificationResult? Overlap);
