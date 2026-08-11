namespace WindowsDownloadManager.Application.RateLimiting;

public sealed class BandwidthController
{
    private readonly object _gate = new();
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> _waiter;
    private readonly long _burst;
    private readonly Dictionary<Guid, TokenBucket> _taskBuckets = [];
    private readonly Dictionary<string, TokenBucket> _domainBuckets = [];
    private readonly TokenBucket? _globalBucket;

    public BandwidthController(
        long? globalBytesPerSecond = null,
        long? perTaskBytesPerSecond = null,
        long? perDomainBytesPerSecond = null,
        long? burstBytes = null,
        Func<DateTimeOffset>? clock = null,
        Func<TimeSpan, CancellationToken, ValueTask>? waiter = null)
    {
        _burst = burstBytes ?? 64 * 1024;
        if (_burst <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(burstBytes));
        }

        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _waiter = waiter ?? (async (delay, token) =>
        {
            await Task.Delay(delay, token).ConfigureAwait(false);
        });

        var now = _clock();
        if (globalBytesPerSecond is { } globalRate)
        {
            _globalBucket = new TokenBucket(globalRate, _burst, now);
        }

        _taskRate = perTaskBytesPerSecond;
        _domainRate = perDomainBytesPerSecond;
    }

    private readonly long? _taskRate;
    private readonly long? _domainRate;

    public async ValueTask AcquireAsync(
        Guid taskId,
        string domain,
        int byteCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domain);
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        if (byteCount == 0)
        {
            return;
        }

        while (true)
        {
            TimeSpan wait;
            lock (_gate)
            {
                wait = ComputeAndConsume(taskId, domain, byteCount);
            }

            if (wait == TimeSpan.Zero)
            {
                return;
            }

            await _waiter(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    private TimeSpan ComputeAndConsume(Guid taskId, string domain, int byteCount)
    {
        var now = _clock();
        var wait = TimeSpan.Zero;

        if (_globalBucket is { } global)
        {
            wait = Max(wait, global.ComputeWait(now, byteCount));
        }

        var taskRate = _taskRate;
        if (taskRate is { })
        {
            var bucket = GetOrCreate(_taskBuckets, taskId, taskRate.Value);
            wait = Max(wait, bucket.ComputeWait(now, byteCount));
        }

        var domainRate = _domainRate;
        if (domainRate is { })
        {
            var bucket = GetOrCreate(_domainBuckets, domain, domainRate.Value);
            wait = Max(wait, bucket.ComputeWait(now, byteCount));
        }

        if (wait == TimeSpan.Zero)
        {
            _globalBucket?.Consume(now, byteCount);
            if (taskRate is { })
            {
                _taskBuckets[taskId].Consume(now, byteCount);
            }

            if (domainRate is { })
            {
                _domainBuckets[domain].Consume(now, byteCount);
            }
        }

        return wait;
    }

    private TokenBucket GetOrCreate(Dictionary<Guid, TokenBucket> buckets, Guid key, long rate)
    {
        if (!buckets.TryGetValue(key, out var bucket))
        {
            bucket = new TokenBucket(rate, _burst, _clock());
            buckets.Add(key, bucket);
        }

        return bucket;
    }

    private TokenBucket GetOrCreate(Dictionary<string, TokenBucket> buckets, string key, long rate)
    {
        if (!buckets.TryGetValue(key, out var bucket))
        {
            bucket = new TokenBucket(rate, _burst, _clock());
            buckets.Add(key, bucket);
        }

        return bucket;
    }

    private static TimeSpan Max(TimeSpan first, TimeSpan second) => first > second ? first : second;

    private sealed class TokenBucket
    {
        private readonly long _rate;
        private readonly long _capacity;
        private double _tokens;
        private DateTimeOffset _lastRefill;

        public TokenBucket(long rate, long capacity, DateTimeOffset now)
        {
            if (rate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rate));
            }

            _rate = rate;
            _capacity = capacity;
            _tokens = capacity;
            _lastRefill = now;
        }

        public TimeSpan ComputeWait(DateTimeOffset now, long requested)
        {
            Refill(now);
            if (_tokens >= requested)
            {
                return TimeSpan.Zero;
            }

            var deficit = requested - _tokens;
            return TimeSpan.FromSeconds(deficit / _rate);
        }

        public void Consume(DateTimeOffset now, long requested)
        {
            Refill(now);
            _tokens -= Math.Min(requested, _tokens);
        }

        private void Refill(DateTimeOffset now)
        {
            var elapsed = now - _lastRefill;
            if (elapsed <= TimeSpan.Zero)
            {
                return;
            }

            _tokens = Math.Min(_capacity, _tokens + elapsed.TotalSeconds * _rate);
            _lastRefill = now;
        }
    }
}
