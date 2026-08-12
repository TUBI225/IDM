using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

/// <summary>
/// Les sept niveaux de reprise dans l'ordre normatif du cahier des charges
/// (Native Range → sondages courts → URL finale autorisée → nouveau lien légitime →
/// recouvrement → retransmission contrôlée → arrêt sûr).
/// </summary>
public enum ForcedResumeLevel
{
    NativeRange = 1,
    ShortProbe = 2,
    AuthorizedFinalUrl = 3,
    NewLink = 4,
    Recovery = 5,
    Retransmission = 6,
    SafeStop = 7,
}

/// <summary>
/// Action que l'orchestrateur doit exécuter pour le niveau retenu.
/// </summary>
public enum ForcedResumeAction
{
    ResumeFromCheckpoint,
    ProbeByteRanges,
    ReanalyzeFinalUrl,
    ValidateAndResumeNewLink,
    ReconcileStorage,
    RetransmitFromZero,
    PreserveAndStop,
}

/// <summary>
/// Raison stable et expurgée de la décision, exploitable par l'interface (W-003).
/// </summary>
public enum ForcedResumeReason
{
    CheckpointResumeSafe,
    ByteRangeCapabilityUnknown,
    RedirectionAuthorized,
    NewLinkToValidate,
    StorageReconciliationRequired,
    ControlledRetransmission,
    RemoteIdentityContradicted,
    RemoteIdentityEvidenceInsufficient,
    NoSafePath,
    UserRequestedStop,
}

/// <summary>
/// Informations disponibles au moment où la reprise doit être décidée.
/// Chaque valeur provient d'une observation vérifiable ; aucune n'est un secret.
/// </summary>
public sealed record ForcedResumeContext(
    Guid DownloadId,
    long ConfirmedBytes,
    bool ResumeMetadataPresent,
    bool RemoteIdentityCompatible,
    bool IdentityContradicted,
    bool IdentityEvidenceInsufficient,
    bool ByteRangeSupportObserved,
    bool ByteRangeSupportLost,
    bool FinalUrlChangedOnly,
    bool LinkExpired,
    bool NewLinkProvided,
    bool RecoveryNeeded,
    bool UserRequestsSafeStop);

/// <summary>
/// Décision immuable du moteur : niveau retenu, action, sûreté, raison et état cible
/// (null si la reprise se poursuit sans transition d'état explicite).
/// </summary>
public sealed record ForcedResumeDecision(
    ForcedResumeLevel Level,
    ForcedResumeAction Action,
    bool CanProceedSafely,
    ForcedResumeReason Reason,
    DownloadState? TargetState = null);

/// <summary>
/// Moteur des sept niveaux de reprise. L'évaluation est ordonnée et ne force jamais :
/// la première branche dont les conditions sont toutes prouvées sûres est retenue, sinon
/// la décision est l'arrêt sûr. Le recouvrement (niveau 5 de l'ordre normatif) est évalué
/// en préalable : aucune reprise réseau n'est sûre sans position réconciliée.
/// La retransmission contrôlée (niveau 6, M-012) est désormais sûre : le moteur la signale
/// avec l'action `RetransmitFromZero`, le coût est annoncé et toute divergence s'arrête.
/// </summary>
public sealed class ForcedResumeEngine
{
    public ForcedResumeDecision Evaluate(ForcedResumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserRequestsSafeStop)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.SafeStop,
                ForcedResumeAction.PreserveAndStop,
                CanProceedSafely: false,
                ForcedResumeReason.UserRequestedStop,
                DownloadState.PermanentFailure);
        }

        if (context.RecoveryNeeded)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.Recovery,
                ForcedResumeAction.ReconcileStorage,
                CanProceedSafely: true,
                ForcedResumeReason.StorageReconciliationRequired,
                TargetState: null);
        }

        if (context.ResumeMetadataPresent &&
            context.RemoteIdentityCompatible &&
            !context.IdentityContradicted &&
            !context.IdentityEvidenceInsufficient &&
            context.ByteRangeSupportObserved &&
            !context.ByteRangeSupportLost &&
            !context.LinkExpired &&
            !context.FinalUrlChangedOnly)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.NativeRange,
                ForcedResumeAction.ResumeFromCheckpoint,
                CanProceedSafely: true,
                ForcedResumeReason.CheckpointResumeSafe,
                TargetState: null);
        }

        if (context.ResumeMetadataPresent &&
            context.RemoteIdentityCompatible &&
            !context.IdentityContradicted &&
            !context.IdentityEvidenceInsufficient &&
            !context.ByteRangeSupportObserved &&
            !context.FinalUrlChangedOnly)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.ShortProbe,
                ForcedResumeAction.ProbeByteRanges,
                CanProceedSafely: true,
                ForcedResumeReason.ByteRangeCapabilityUnknown,
                DownloadState.ProbingRange);
        }

        if (context.ResumeMetadataPresent &&
            context.FinalUrlChangedOnly &&
            !context.IdentityContradicted &&
            !context.IdentityEvidenceInsufficient)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.AuthorizedFinalUrl,
                ForcedResumeAction.ReanalyzeFinalUrl,
                CanProceedSafely: true,
                ForcedResumeReason.RedirectionAuthorized,
                DownloadState.ProbingRange);
        }

        if (context.LinkExpired &&
            context.NewLinkProvided &&
            !context.IdentityContradicted)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.NewLink,
                ForcedResumeAction.ValidateAndResumeNewLink,
                CanProceedSafely: true,
                ForcedResumeReason.NewLinkToValidate,
                DownloadState.RenewingLink);
        }

        if (!context.ResumeMetadataPresent || context.ByteRangeSupportLost)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.Retransmission,
                ForcedResumeAction.RetransmitFromZero,
                CanProceedSafely: true,
                ForcedResumeReason.ControlledRetransmission,
                DownloadState.Retransmitting);
        }

        return SafeStop(context);
    }

    private static ForcedResumeDecision SafeStop(ForcedResumeContext context)
    {
        if (context.IdentityContradicted)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.SafeStop,
                ForcedResumeAction.PreserveAndStop,
                CanProceedSafely: false,
                ForcedResumeReason.RemoteIdentityContradicted,
                DownloadState.RemoteFileChanged);
        }

        if (context.IdentityEvidenceInsufficient)
        {
            return new ForcedResumeDecision(
                ForcedResumeLevel.SafeStop,
                ForcedResumeAction.PreserveAndStop,
                CanProceedSafely: false,
                ForcedResumeReason.RemoteIdentityEvidenceInsufficient,
                DownloadState.RemoteFileChanged);
        }

        return new ForcedResumeDecision(
            ForcedResumeLevel.SafeStop,
            ForcedResumeAction.PreserveAndStop,
            CanProceedSafely: false,
            ForcedResumeReason.NoSafePath,
            DownloadState.PermanentFailure);
    }
}

