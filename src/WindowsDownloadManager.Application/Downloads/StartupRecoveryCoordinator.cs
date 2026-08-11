using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class StartupRecoveryCoordinator
{
    private readonly StartupRecoveryReconciler _temporaryFileReconciler;
    private readonly RemoteIdentityReconciler _remoteIdentityReconciler;
    private readonly RecoveryDecisionEvaluator _decisionEvaluator;
    private readonly RecoveryOverlapVerifier _overlapVerifier;

    public StartupRecoveryCoordinator(
        StartupRecoveryReconciler temporaryFileReconciler,
        RemoteIdentityReconciler remoteIdentityReconciler,
        RecoveryDecisionEvaluator decisionEvaluator,
        RecoveryOverlapVerifier overlapVerifier)
    {
        _temporaryFileReconciler = temporaryFileReconciler ??
            throw new ArgumentNullException(nameof(temporaryFileReconciler));
        _remoteIdentityReconciler = remoteIdentityReconciler ??
            throw new ArgumentNullException(nameof(remoteIdentityReconciler));
        _decisionEvaluator = decisionEvaluator ??
            throw new ArgumentNullException(nameof(decisionEvaluator));
        _overlapVerifier = overlapVerifier ??
            throw new ArgumentNullException(nameof(overlapVerifier));
    }

    public async ValueTask<StartupRecoveryAssessmentResult> CoordinateAsync(
        DownloadTask task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        var temporaryFile = await _temporaryFileReconciler
            .ReconcileAsync(task, cancellationToken)
            .ConfigureAwait(false);
        var localBlockers = _decisionEvaluator.EvaluateLocalBlockers(temporaryFile);
        if (localBlockers != RecoveryBlocker.None)
        {
            return new StartupRecoveryAssessmentResult(
                task.Id,
                StartupRecoveryAssessmentStatus.BlockedBeforeRemoteAnalysis,
                localBlockers,
                temporaryFile,
                RemoteIdentity: null,
                Decision: null,
                Overlap: null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var remoteIdentity = await _remoteIdentityReconciler
            .ReconcileAsync(task, cancellationToken)
            .ConfigureAwait(false);
        var decision = _decisionEvaluator.Evaluate(temporaryFile, remoteIdentity);
        if (decision.Status == RecoveryDecisionStatus.Blocked)
        {
            return new StartupRecoveryAssessmentResult(
                task.Id,
                StartupRecoveryAssessmentStatus.BlockedAfterRemoteAnalysis,
                decision.Blockers,
                temporaryFile,
                remoteIdentity,
                decision,
                Overlap: null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var overlap = await _overlapVerifier
            .VerifyAsync(decision, cancellationToken)
            .ConfigureAwait(false);

        return new StartupRecoveryAssessmentResult(
            task.Id,
            Map(overlap.Status),
            decision.Blockers,
            temporaryFile,
            remoteIdentity,
            decision,
            overlap);
    }

    private static StartupRecoveryAssessmentStatus Map(OverlapVerificationStatus status) => status switch
    {
        OverlapVerificationStatus.NotRequired => StartupRecoveryAssessmentStatus.OverlapNotRequired,
        OverlapVerificationStatus.Match => StartupRecoveryAssessmentStatus.OverlapMatched,
        OverlapVerificationStatus.Mismatch => StartupRecoveryAssessmentStatus.OverlapMismatched,
        OverlapVerificationStatus.LocalFileChanged =>
            StartupRecoveryAssessmentStatus.LocalFileChangedDuringOverlap,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown overlap status."),
    };
}
