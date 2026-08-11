using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Application.Retries;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly ITransientFailureClassifier _classifier;
    private readonly int _maxAttempts;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly Random _random;

    public ExponentialBackoffRetryPolicy(
        ITransientFailureClassifier classifier,
        int maxAttempts = 5,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        Random? random = null)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        _maxAttempts = maxAttempts;
        _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(250);
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        _random = random ?? new Random();
    }

    public RetryDecision Evaluate(int attempt, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        if (!_classifier.IsTransient(exception) || attempt >= _maxAttempts)
        {
            return default;
        }

        var retryAfter = _classifier.GetRetryAfter(exception);
        var delay = retryAfter ?? ComputeBackoffDelay(attempt);
        return new RetryDecision(ShouldRetry: true, Cap(delay));
    }

    private TimeSpan ComputeBackoffDelay(int attempt)
    {
        var exponent = Math.Min(attempt - 1, 8);
        var multiplier = Math.Pow(2, exponent);
        var jitter = _random.NextDouble() * 0.5 + 0.5; // 50 % à 100 % de la base exponentielle
        var ticks = _baseDelay.Ticks * multiplier * jitter;
        return TimeSpan.FromTicks((long)Math.Min(ticks, _maxDelay.Ticks));
    }

    private TimeSpan Cap(TimeSpan delay) => delay > _maxDelay ? _maxDelay : delay;
}
