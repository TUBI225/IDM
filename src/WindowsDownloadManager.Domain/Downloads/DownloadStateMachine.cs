namespace WindowsDownloadManager.Domain.Downloads;

public static class DownloadStateMachine
{
    private static readonly IReadOnlyDictionary<DownloadState, IReadOnlySet<DownloadState>> Allowed =
        new Dictionary<DownloadState, IReadOnlySet<DownloadState>>
        {
            [DownloadState.New] = Set(DownloadState.Analyzing, DownloadState.Cancelled),
            [DownloadState.Analyzing] = Set(DownloadState.Preparing, DownloadState.PermanentFailure, DownloadState.AuthenticationRequired),
            [DownloadState.Preparing] = Set(DownloadState.Waiting, DownloadState.InsufficientDiskSpace, DownloadState.DestinationUnavailable),
            [DownloadState.Waiting] = Set(DownloadState.Downloading, DownloadState.Cancelled),
            [DownloadState.Downloading] = Set(DownloadState.PauseRequested, DownloadState.Reconnecting, DownloadState.Verifying, DownloadState.InsufficientDiskSpace, DownloadState.Cancelled),
            [DownloadState.PauseRequested] = Set(DownloadState.Paused),
            [DownloadState.Paused] = Set(DownloadState.Analyzing, DownloadState.Cancelled),
            [DownloadState.Reconnecting] = Set(DownloadState.TestingResume, DownloadState.TemporaryFailure, DownloadState.LinkExpired),
            [DownloadState.TestingResume] = Set(DownloadState.ProbingRange, DownloadState.RenewingLink, DownloadState.Retransmitting, DownloadState.RemoteFileChanged, DownloadState.PermanentFailure),
            [DownloadState.ProbingRange] = Set(DownloadState.Downloading, DownloadState.RenewingLink, DownloadState.Retransmitting, DownloadState.UnreliableRangeServer),
            [DownloadState.RenewingLink] = Set(DownloadState.TestingResume, DownloadState.AuthenticationRequired, DownloadState.RemoteFileChanged),
            [DownloadState.Retransmitting] = Set(DownloadState.Downloading, DownloadState.RemoteFileChanged, DownloadState.PauseRequested),
            [DownloadState.Verifying] = Set(DownloadState.Finalizing, DownloadState.RemoteFileChanged, DownloadState.PermanentFailure),
            [DownloadState.Finalizing] = Set(DownloadState.Completed, DownloadState.DestinationUnavailable),
            [DownloadState.TemporaryFailure] = Set(DownloadState.Reconnecting, DownloadState.Cancelled),
            [DownloadState.LinkExpired] = Set(DownloadState.RenewingLink, DownloadState.Cancelled),
            [DownloadState.AuthenticationRequired] = Set(DownloadState.Analyzing, DownloadState.Cancelled),
            [DownloadState.InsufficientDiskSpace] = Set(DownloadState.Preparing, DownloadState.Cancelled),
            [DownloadState.DestinationUnavailable] = Set(DownloadState.Preparing, DownloadState.Cancelled),
            [DownloadState.UnreliableRangeServer] = Set(DownloadState.Retransmitting, DownloadState.PermanentFailure),
        };

    public static bool CanTransition(DownloadState current, DownloadState next) =>
        Allowed.TryGetValue(current, out var targets) && targets.Contains(next);

    private static IReadOnlySet<DownloadState> Set(params DownloadState[] states) =>
        new HashSet<DownloadState>(states);
}
