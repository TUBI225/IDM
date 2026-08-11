namespace WindowsDownloadManager.Domain.Downloads;

public sealed record RemoteIdentity
{
    public RemoteIdentity(
        Uri finalUri,
        long? length,
        string? entityTag,
        DateTimeOffset? lastModified,
        bool supportsByteRanges)
    {
        ArgumentNullException.ThrowIfNull(finalUri);
        if (!finalUri.IsAbsoluteUri || finalUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("The final URI must use HTTP or HTTPS.", nameof(finalUri));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        FinalUri = finalUri;
        Length = length;
        EntityTag = string.IsNullOrWhiteSpace(entityTag) ? null : entityTag;
        LastModified = lastModified;
        SupportsByteRanges = supportsByteRanges;
    }

    public Uri FinalUri { get; }
    public long? Length { get; }
    public string? EntityTag { get; }
    public DateTimeOffset? LastModified { get; }
    public bool SupportsByteRanges { get; }
}
