namespace WindowsDownloadManager.Application.Abstractions;

public interface IRemoteResourceAnalyzer
{
    ValueTask<RemoteResourceInfo> AnalyzeAsync(Uri uri, CancellationToken cancellationToken);
}

public sealed record RemoteResourceInfo(
    Uri OriginalUri,
    Uri FinalUri,
    long? Length,
    string? SuggestedFileName,
    string? ContentType,
    string? EntityTag,
    DateTimeOffset? LastModified,
    bool SupportsByteRanges,
    string? Sha256 = null);
