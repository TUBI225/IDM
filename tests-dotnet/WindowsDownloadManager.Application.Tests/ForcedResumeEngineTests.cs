using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class ForcedResumeEngineTests
{
    private static readonly ForcedResumeEngine Engine = new();
    private static readonly Guid DownloadId = Guid.NewGuid();

    [TestMethod]
    public void Evaluate_CompatibleMetadataWithByteRanges_ChoosesNativeRange()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.NativeRange, decision.Level);
        Assert.AreEqual(ForcedResumeAction.ResumeFromCheckpoint, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.CheckpointResumeSafe, decision.Reason);
        Assert.IsNull(decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_CompatibleButUnknownRange_ChoosesShortProbe()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: false);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.ShortProbe, decision.Level);
        Assert.AreEqual(ForcedResumeAction.ProbeByteRanges, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.ByteRangeCapabilityUnknown, decision.Reason);
        Assert.AreEqual(DownloadState.ProbingRange, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_OnlyFinalUrlChanged_ChoosesAuthorizedFinalUrl()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            finalUrlChangedOnly: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.AuthorizedFinalUrl, decision.Level);
        Assert.AreEqual(ForcedResumeAction.ReanalyzeFinalUrl, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.RedirectionAuthorized, decision.Reason);
        Assert.AreEqual(DownloadState.ProbingRange, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_LinkExpiredWithNewLink_ChoosesNewLink()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            linkExpired: true,
            newLinkProvided: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.NewLink, decision.Level);
        Assert.AreEqual(ForcedResumeAction.ValidateAndResumeNewLink, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.NewLinkToValidate, decision.Reason);
        Assert.AreEqual(DownloadState.RenewingLink, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_NewLinkButIdentityContradicted_RefusesToResume()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            linkExpired: true,
            newLinkProvided: true,
            identityContradicted: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.SafeStop, decision.Level);
        Assert.AreEqual(ForcedResumeAction.PreserveAndStop, decision.Action);
        Assert.IsFalse(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.RemoteIdentityContradicted, decision.Reason);
        Assert.AreEqual(DownloadState.RemoteFileChanged, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_RecoveryNeeded_ChoosesRecoveryBeforeAnyResume()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            recoveryNeeded: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.Recovery, decision.Level);
        Assert.AreEqual(ForcedResumeAction.ReconcileStorage, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.StorageReconciliationRequired, decision.Reason);
        Assert.IsNull(decision.TargetState);
    }
    [TestMethod]
    public void Evaluate_MetadataAbsent_ChoosesControlledRetransmission()
    {
        var context = Context(
            resumeMetadataPresent: false,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.Retransmission, decision.Level);
        Assert.AreEqual(ForcedResumeAction.RetransmitFromZero, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.ControlledRetransmission, decision.Reason);
        Assert.AreEqual(DownloadState.Retransmitting, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_ByteRangeSupportLost_ChoosesControlledRetransmission()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            byteRangeSupportLost: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.Retransmission, decision.Level);
        Assert.AreEqual(ForcedResumeAction.RetransmitFromZero, decision.Action);
        Assert.IsTrue(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.ControlledRetransmission, decision.Reason);
        Assert.AreEqual(DownloadState.Retransmitting, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_IdentityContradicted_StopsSafelyWithRemoteFileChanged()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            identityContradicted: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.SafeStop, decision.Level);
        Assert.AreEqual(ForcedResumeAction.PreserveAndStop, decision.Action);
        Assert.IsFalse(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.RemoteIdentityContradicted, decision.Reason);
        Assert.AreEqual(DownloadState.RemoteFileChanged, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_IdentityContradictedWithTransientSignals_NeverChoosesNativeRange()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            identityContradicted: true);

        var decision = Engine.Evaluate(context);

        Assert.AreNotEqual(ForcedResumeLevel.NativeRange, decision.Level);
        Assert.IsFalse(decision.CanProceedSafely);
    }


    [TestMethod]
    public void Evaluate_EvidenceInsufficient_StopsSafelyWithRemoteFileChanged()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            identityEvidenceInsufficient: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.SafeStop, decision.Level);
        Assert.AreEqual(ForcedResumeAction.PreserveAndStop, decision.Action);
        Assert.IsFalse(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.RemoteIdentityEvidenceInsufficient, decision.Reason);
        Assert.AreEqual(DownloadState.RemoteFileChanged, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_UserRequestsSafeStop_AlwaysStopsSafely()
    {
        var context = Context(
            resumeMetadataPresent: true,
            remoteIdentityCompatible: true,
            byteRangeSupportObserved: true,
            recoveryNeeded: true,
            userRequestsSafeStop: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.SafeStop, decision.Level);
        Assert.AreEqual(ForcedResumeAction.PreserveAndStop, decision.Action);
        Assert.IsFalse(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.UserRequestedStop, decision.Reason);
        Assert.AreEqual(DownloadState.PermanentFailure, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_NoSafePath_ChoosesSafeStopPermanentFailure()
    {
        var context = Context(resumeMetadataPresent: true);

        var decision = Engine.Evaluate(context);

        Assert.AreEqual(ForcedResumeLevel.SafeStop, decision.Level);
        Assert.AreEqual(ForcedResumeAction.PreserveAndStop, decision.Action);
        Assert.IsFalse(decision.CanProceedSafely);
        Assert.AreEqual(ForcedResumeReason.NoSafePath, decision.Reason);
        Assert.AreEqual(DownloadState.PermanentFailure, decision.TargetState);
    }

    [TestMethod]
    public void Evaluate_TargetStates_AreLegalTransitionsFromTestingResume()
    {
        var scenarios = new (ForcedResumeLevel Level, DownloadState? TargetState)[]
        {
            (ForcedResumeLevel.NativeRange, null),
            (ForcedResumeLevel.ShortProbe, DownloadState.ProbingRange),
            (ForcedResumeLevel.AuthorizedFinalUrl, DownloadState.ProbingRange),
            (ForcedResumeLevel.NewLink, DownloadState.RenewingLink),
            (ForcedResumeLevel.Recovery, null),
            (ForcedResumeLevel.Retransmission, DownloadState.Retransmitting),
            (ForcedResumeLevel.SafeStop, DownloadState.RemoteFileChanged),
            (ForcedResumeLevel.SafeStop, DownloadState.PermanentFailure),
        };

        foreach (var scenario in scenarios)
        {
            if (scenario.TargetState is { } target)
            {
                Assert.IsTrue(
                    DownloadStateMachine.CanTransition(DownloadState.TestingResume, target),
                    $"La transition {DownloadState.TestingResume} -> {target} doit être légale.");
            }
        }
    }

    [TestMethod]
    public void Evaluate_NullContext_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Engine.Evaluate(null!));
    }

    private static ForcedResumeContext Context(
        bool resumeMetadataPresent = false,
        bool remoteIdentityCompatible = false,
        bool identityContradicted = false,
        bool identityEvidenceInsufficient = false,
        bool byteRangeSupportObserved = false,
        bool byteRangeSupportLost = false,
        bool finalUrlChangedOnly = false,
        bool linkExpired = false,
        bool newLinkProvided = false,
        bool recoveryNeeded = false,
        bool userRequestsSafeStop = false) =>
        new(
            DownloadId,
            ConfirmedBytes: 512,
            resumeMetadataPresent,
            remoteIdentityCompatible,
            identityContradicted,
            identityEvidenceInsufficient,
            byteRangeSupportObserved,
            byteRangeSupportLost,
            finalUrlChangedOnly,
            linkExpired,
            newLinkProvided,
            recoveryNeeded,
            userRequestsSafeStop);
}

