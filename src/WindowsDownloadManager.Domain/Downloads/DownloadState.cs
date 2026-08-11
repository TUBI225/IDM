namespace WindowsDownloadManager.Domain.Downloads;

public enum DownloadState
{
    New,
    Analyzing,
    Preparing,
    Waiting,
    Downloading,
    PauseRequested,
    Paused,
    Reconnecting,
    TestingResume,
    ProbingRange,
    RenewingLink,
    Retransmitting,
    Verifying,
    Finalizing,
    Completed,
    TemporaryFailure,
    PermanentFailure,
    LinkExpired,
    RemoteFileChanged,
    InsufficientDiskSpace,
    DestinationUnavailable,
    AuthenticationRequired,
    UnreliableRangeServer,
    Cancelled
}
