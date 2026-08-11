namespace WindowsDownloadManager.Application.Downloads;

public sealed class RecoveryDecisionEvaluator
{
    public RecoveryDecisionResult Evaluate(
        TemporaryFileReconciliationResult temporaryFile,
        RemoteIdentityReconciliationResult remoteIdentity)
    {
        ArgumentNullException.ThrowIfNull(temporaryFile);
        ArgumentNullException.ThrowIfNull(remoteIdentity);

        if (temporaryFile.DownloadId != remoteIdentity.DownloadId)
        {
            throw new ArgumentException(
                "Recovery reconciliation results must belong to the same download.",
                nameof(remoteIdentity));
        }

        var blockers = EvaluateLocalBlockers(temporaryFile) |
            EvaluateRemoteIdentity(remoteIdentity.Status);
        var status = blockers == RecoveryBlocker.None
            ? RecoveryDecisionStatus.ReadyForOverlapVerification
            : RecoveryDecisionStatus.Blocked;

        return new RecoveryDecisionResult(
            temporaryFile.DownloadId,
            status,
            blockers,
            temporaryFile.SafePosition,
            temporaryFile,
            remoteIdentity);
    }

    public RecoveryBlocker EvaluateLocalBlockers(
        TemporaryFileReconciliationResult temporaryFile)
    {
        ArgumentNullException.ThrowIfNull(temporaryFile);

        return temporaryFile.Status switch
        {
            TemporaryFileReconciliationStatus.RecoveryMetadataAbsent =>
                RecoveryBlocker.RecoveryMetadataAbsent,
            TemporaryFileReconciliationStatus.TemporaryFileAbsent =>
                RecoveryBlocker.TemporaryFileAbsent,
            TemporaryFileReconciliationStatus.TemporaryFileShorter =>
                RecoveryBlocker.CheckpointAheadOfTemporaryFile,
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint =>
                RecoveryBlocker.None,
            TemporaryFileReconciliationStatus.TemporaryFileLonger =>
                RecoveryBlocker.UnconfirmedTemporaryFileTail,
            _ => throw new ArgumentOutOfRangeException(
                nameof(temporaryFile),
                temporaryFile.Status,
                "Unknown temporary-file status."),
        };
    }

    private static RecoveryBlocker EvaluateRemoteIdentity(
        RemoteIdentityReconciliationStatus status) => status switch
        {
            RemoteIdentityReconciliationStatus.RecoveryMetadataAbsent =>
                RecoveryBlocker.RecoveryMetadataAbsent,
            RemoteIdentityReconciliationStatus.Compatible =>
                RecoveryBlocker.None,
            RemoteIdentityReconciliationStatus.InsufficientEvidence =>
                RecoveryBlocker.RemoteIdentityEvidenceInsufficient,
            RemoteIdentityReconciliationStatus.ResumeCapabilityLost =>
                RecoveryBlocker.ByteRangeResumeUnavailable,
            RemoteIdentityReconciliationStatus.Contradictory =>
                RecoveryBlocker.RemoteIdentityContradictory,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown remote-identity status."),
        };
}
