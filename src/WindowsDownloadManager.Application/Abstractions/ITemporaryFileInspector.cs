namespace WindowsDownloadManager.Application.Abstractions;

public interface ITemporaryFileInspector
{
    ValueTask<TemporaryFileSnapshot> InspectAsync(
        string temporaryPath,
        CancellationToken cancellationToken);
}

public sealed class TemporaryFileSnapshot
{
    private TemporaryFileSnapshot(bool exists, long? length)
    {
        Exists = exists;
        Length = length;
    }

    public bool Exists { get; }
    public long? Length { get; }

    public static TemporaryFileSnapshot Absent { get; } = new(false, null);

    public static TemporaryFileSnapshot Existing(long length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return new TemporaryFileSnapshot(true, length);
    }
}
