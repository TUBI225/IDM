namespace WindowsDownloadManager.Domain.Downloads;

public sealed class DownloadTask
{
    public DownloadTask(Guid id, Uri originalUri, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(originalUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (originalUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Only HTTP and HTTPS are supported.", nameof(originalUri));
        }

        Id = id;
        OriginalUri = originalUri;
        DestinationPath = destinationPath;
    }

    public Guid Id { get; }
    public Uri OriginalUri { get; }
    public string DestinationPath { get; private set; }
    public DownloadState State { get; private set; } = DownloadState.New;
    public long ConfirmedBytes { get; private set; }
    public string? TemporaryPath { get; private set; }
    public RemoteIdentity? RemoteIdentity { get; private set; }
    public string? VerifiedSha256 { get; private set; }

    public static DownloadTask Restore(
        Guid id,
        Uri originalUri,
        string destinationPath,
        DownloadState state,
        long confirmedBytes,
        string? temporaryPath = null,
        RemoteIdentity? remoteIdentity = null,
        string? verifiedSha256 = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (confirmedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(confirmedBytes));
        }

        if ((temporaryPath is null) != (remoteIdentity is null))
        {
            throw new InvalidDataException("The temporary path and remote identity must be restored together.");
        }

        if (temporaryPath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        }

        var normalizedSha256 = verifiedSha256 is null
            ? null
            : Sha256Hex.Normalize(verifiedSha256, nameof(verifiedSha256));
        if (normalizedSha256 is not null &&
            state is not (DownloadState.Verifying or DownloadState.Finalizing or DownloadState.Completed))
        {
            throw new InvalidDataException("A verified SHA-256 is only valid during or after verification.");
        }

        return new DownloadTask(id, originalUri, destinationPath)
        {
            State = state,
            ConfirmedBytes = confirmedBytes,
            TemporaryPath = temporaryPath,
            RemoteIdentity = remoteIdentity,
            VerifiedSha256 = normalizedSha256,
        };
    }

    public void RecordPreparation(string temporaryPath, RemoteIdentity remoteIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        ArgumentNullException.ThrowIfNull(remoteIdentity);
        if (State != DownloadState.Preparing)
        {
            throw new InvalidOperationException("Preparation metadata can only be recorded in Preparing state.");
        }

        if (TemporaryPath is not null || RemoteIdentity is not null)
        {
            throw new InvalidOperationException("Preparation metadata has already been recorded.");
        }

        TemporaryPath = temporaryPath;
        RemoteIdentity = remoteIdentity;
    }

    public void TransitionTo(DownloadState next)
    {
        if (State == DownloadState.Verifying &&
            next == DownloadState.Finalizing &&
            VerifiedSha256 is null)
        {
            throw new InvalidOperationException("A SHA-256 verification is required before finalization.");
        }

        if (!DownloadStateMachine.CanTransition(State, next))
        {
            throw new InvalidOperationException($"Invalid transition: {State} -> {next}.");
        }

        State = next;
    }

    public void RecordVerifiedSha256(string sha256)
    {
        if (State != DownloadState.Verifying)
        {
            throw new InvalidOperationException("SHA-256 can only be recorded in Verifying state.");
        }

        if (VerifiedSha256 is not null)
        {
            throw new InvalidOperationException("SHA-256 has already been recorded.");
        }

        VerifiedSha256 = Sha256Hex.Normalize(sha256, nameof(sha256));
    }

    public void ResolveDestinationCollision(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (State != DownloadState.Verifying)
        {
            throw new InvalidOperationException("A destination collision can only be resolved in Verifying state.");
        }

        if (VerifiedSha256 is not null)
        {
            throw new InvalidOperationException("The destination cannot change after SHA-256 verification.");
        }

        if (string.Equals(DestinationPath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The resolved destination must differ from the current destination.", nameof(destinationPath));
        }

        DestinationPath = destinationPath;
    }

    public void ConfirmPersistedBytes(long confirmedBytes)
    {
        if (confirmedBytes < ConfirmedBytes)
        {
            throw new InvalidOperationException("Confirmed progress cannot move backwards.");
        }

        ConfirmedBytes = confirmedBytes;
    }
}
