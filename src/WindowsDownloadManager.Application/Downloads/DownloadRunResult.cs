using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed record DownloadRunResult(
    Guid DownloadId,
    string TemporaryPath,
    long ConfirmedBytes,
    DownloadState State,
    RemoteResourceInfo Resource);
