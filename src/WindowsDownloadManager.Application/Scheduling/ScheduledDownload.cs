namespace WindowsDownloadManager.Application.Scheduling;

public sealed record ScheduledDownload(
    Guid DownloadId,
    int Priority,
    DateTimeOffset SubmittedAt)
{
    public int CompareTo(ScheduledDownload other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var byPriority = other.Priority.CompareTo(Priority); // priorité haute d'abord
        return byPriority != 0 ? byPriority : SubmittedAt.CompareTo(other.SubmittedAt);
    }
}
