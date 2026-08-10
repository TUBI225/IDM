using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Abstractions;

public interface IDownloadRepository
{
    ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken);
    ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken);
}
