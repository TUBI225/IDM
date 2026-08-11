namespace WindowsDownloadManager.Application.Abstractions;

public interface ITransientFailureClassifier
{
    bool IsTransient(Exception exception);
    TimeSpan? GetRetryAfter(Exception exception);
}
