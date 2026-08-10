using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class RecoveryDecisionEvaluatorTests
{
    [TestMethod]
    public void Evaluate_MatchingTemporaryFileAndCompatibleRemote_IsReadyForOverlapVerification()
    {
        var local = Local(TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint);

        var result = new RecoveryDecisionEvaluator().Evaluate(
            local,
            Remote(local.DownloadId, RemoteIdentityReconciliationStatus.Compatible));

        Assert.AreEqual(RecoveryDecisionStatus.ReadyForOverlapVerification, result.Status);
        Assert.AreEqual(RecoveryBlocker.None, result.Blockers);
        Assert.AreEqual(5, result.SafePosition);
        Assert.AreSame(local, result.TemporaryFile);
    }

    [TestMethod]
    public void Evaluate_LocalRecoveryMetadataAbsent_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.RecoveryMetadataAbsent,
            RemoteIdentityReconciliationStatus.Compatible,
            RecoveryBlocker.RecoveryMetadataAbsent);
    }

    [TestMethod]
    public void Evaluate_RemoteRecoveryMetadataAbsent_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            RemoteIdentityReconciliationStatus.RecoveryMetadataAbsent,
            RecoveryBlocker.RecoveryMetadataAbsent);
    }

    [TestMethod]
    public void Evaluate_TemporaryFileAbsent_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileAbsent,
            RemoteIdentityReconciliationStatus.Compatible,
            RecoveryBlocker.TemporaryFileAbsent);
    }

    [TestMethod]
    public void Evaluate_CheckpointAheadOfTemporaryFile_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileShorter,
            RemoteIdentityReconciliationStatus.Compatible,
            RecoveryBlocker.CheckpointAheadOfTemporaryFile);
    }

    [TestMethod]
    public void Evaluate_UnconfirmedTemporaryFileTail_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            RemoteIdentityReconciliationStatus.Compatible,
            RecoveryBlocker.UnconfirmedTemporaryFileTail);
    }

    [TestMethod]
    public void Evaluate_ContradictoryRemoteIdentity_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            RemoteIdentityReconciliationStatus.Contradictory,
            RecoveryBlocker.RemoteIdentityContradictory);
    }

    [TestMethod]
    public void Evaluate_InsufficientRemoteEvidence_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            RemoteIdentityReconciliationStatus.InsufficientEvidence,
            RecoveryBlocker.RemoteIdentityEvidenceInsufficient);
    }

    [TestMethod]
    public void Evaluate_ByteRangeCapabilityLost_IsBlocked()
    {
        AssertBlocked(
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            RemoteIdentityReconciliationStatus.ResumeCapabilityLost,
            RecoveryBlocker.ByteRangeResumeUnavailable);
    }

    [TestMethod]
    public void Evaluate_MultipleProblems_AggregatesEveryBlocker()
    {
        var local = Local(TemporaryFileReconciliationStatus.TemporaryFileShorter);

        var result = new RecoveryDecisionEvaluator().Evaluate(
            local,
            Remote(local.DownloadId, RemoteIdentityReconciliationStatus.Contradictory));

        Assert.AreEqual(RecoveryDecisionStatus.Blocked, result.Status);
        Assert.AreEqual(
            RecoveryBlocker.CheckpointAheadOfTemporaryFile |
            RecoveryBlocker.RemoteIdentityContradictory,
            result.Blockers);
    }

    [TestMethod]
    public void Evaluate_DifferentDownloadIds_ThrowsWithoutDecision()
    {
        var local = Local(TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint);

        Assert.ThrowsExactly<ArgumentException>(() =>
            new RecoveryDecisionEvaluator().Evaluate(
                local,
                Remote(Guid.NewGuid(), RemoteIdentityReconciliationStatus.Compatible)));
    }

    private static void AssertBlocked(
        TemporaryFileReconciliationStatus localStatus,
        RemoteIdentityReconciliationStatus remoteStatus,
        RecoveryBlocker expectedBlocker)
    {
        var local = Local(localStatus);

        var result = new RecoveryDecisionEvaluator().Evaluate(
            local,
            Remote(local.DownloadId, remoteStatus));

        Assert.AreEqual(RecoveryDecisionStatus.Blocked, result.Status);
        Assert.AreEqual(expectedBlocker, result.Blockers);
    }

    private static TemporaryFileReconciliationResult Local(
        TemporaryFileReconciliationStatus status) =>
        new(
            Guid.NewGuid(),
            status,
            status == TemporaryFileReconciliationStatus.RecoveryMetadataAbsent
                ? null
                : "C:\\Downloads\\file.download",
            ConfirmedBytes: 5,
            FileLength: status == TemporaryFileReconciliationStatus.TemporaryFileAbsent
                ? null
                : 5,
            SafePosition: status is TemporaryFileReconciliationStatus.RecoveryMetadataAbsent or
                TemporaryFileReconciliationStatus.TemporaryFileAbsent
                ? 0
                : 5);

    private static RemoteIdentityReconciliationResult Remote(
        Guid downloadId,
        RemoteIdentityReconciliationStatus status) =>
        new(
            downloadId,
            status,
            RemoteIdentityDifference.None,
            PersistedIdentity: null,
            ObservedIdentity: null);
}
