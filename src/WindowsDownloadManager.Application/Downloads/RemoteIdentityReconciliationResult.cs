using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public enum RemoteIdentityReconciliationStatus
{
    RecoveryMetadataAbsent,
    Compatible,
    InsufficientEvidence,
    ResumeCapabilityLost,
    Contradictory,
}

[Flags]
public enum RemoteIdentityDifference
{
    None = 0,
    FinalUriChanged = 1 << 0,
    LengthChanged = 1 << 1,
    EntityTagChanged = 1 << 2,
    LastModifiedChanged = 1 << 3,
    LengthEvidenceMissing = 1 << 4,
    EntityTagEvidenceMissing = 1 << 5,
    LastModifiedEvidenceMissing = 1 << 6,
    SufficientIdentityEvidenceMissing = 1 << 7,
    ByteRangeSupportLost = 1 << 8,
}

public sealed record RemoteIdentityReconciliationResult(
    Guid DownloadId,
    RemoteIdentityReconciliationStatus Status,
    RemoteIdentityDifference Differences,
    RemoteIdentity? PersistedIdentity,
    RemoteIdentity? ObservedIdentity);
