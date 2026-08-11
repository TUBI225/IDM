namespace WindowsDownloadManager.Domain.Downloads;

public sealed record RemoteIdentity
{
    public RemoteIdentity(
        Uri finalUri,
        long? length,
        string? entityTag,
        DateTimeOffset? lastModified,
        bool supportsByteRanges,
        string? sha256 = null)
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

        if (sha256 is not null)
        {
            sha256 = Sha256Hex.Normalize(sha256, nameof(sha256));
        }

        FinalUri = finalUri;
        Length = length;
        EntityTag = string.IsNullOrWhiteSpace(entityTag) ? null : entityTag;
        LastModified = lastModified;
        SupportsByteRanges = supportsByteRanges;
        Sha256 = sha256;
    }

    public Uri FinalUri { get; }
    public long? Length { get; }
    public string? EntityTag { get; }
    public DateTimeOffset? LastModified { get; }
    public bool SupportsByteRanges { get; }
    public string? Sha256 { get; }
}
