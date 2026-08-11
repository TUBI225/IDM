namespace WindowsDownloadManager.Application.Retries;

public readonly record struct RetryDecision(bool ShouldRetry, TimeSpan Delay);

public interface IRetryPolicy
{
    RetryDecision Evaluate(int attempt, Exception exception);
}
