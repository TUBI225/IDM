namespace WindowsDownloadManager.Application.Abstractions;

public interface IRemoteBoundedContentSource
{
    ValueTask<RemoteContentLease> OpenBoundedReadAsync(
        RemoteResourceInfo resource,
        long start,
        long end,
        CancellationToken cancellationToken);
}
