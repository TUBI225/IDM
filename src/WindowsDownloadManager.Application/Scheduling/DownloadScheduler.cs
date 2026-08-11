namespace WindowsDownloadManager.Application.Scheduling;

public sealed class DownloadScheduler
{
    private readonly int _maxConcurrent;
    private readonly TimeSpan _agingInterval;
    private readonly int _agingBoost;
    private readonly object _gate = new();
    private readonly List<ScheduledDownload> _pending = [];
    private readonly HashSet<Guid> _active = [];

    public DownloadScheduler(int maxConcurrent, TimeSpan? agingInterval = null, int agingBoost = 0)
    {
        if (maxConcurrent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrent));
        }

        if (agingInterval is { } interval && interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(agingInterval));
        }

        if (agingBoost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(agingBoost));
        }

        _maxConcurrent = maxConcurrent;
        _agingInterval = agingInterval ?? TimeSpan.FromSeconds(30);
        _agingBoost = agingBoost;
    }

    public int PendingCount
    {
        get
        {
            lock (_gate)
            {
                return _pending.Count;
            }
        }
    }

    public int ActiveCount
    {
        get
        {
            lock (_gate)
            {
                return _active.Count;
            }
        }
    }

    public bool IsActive(Guid downloadId)
    {
        lock (_gate)
        {
            return _active.Contains(downloadId);
        }
    }

    public void Submit(ScheduledDownload download)
    {
        ArgumentNullException.ThrowIfNull(download);
        lock (_gate)
        {
            _pending.Add(download);
        }
    }

    public ScheduledDownload? AcquireNext(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_active.Count >= _maxConcurrent || _pending.Count == 0)
            {
                return null;
            }

            var next = SelectNext(_pending, now);
            _pending.Remove(next);
            _active.Add(next.DownloadId);
            return next;
        }
    }

    public void Release(Guid downloadId)
    {
        lock (_gate)
        {
            _active.Remove(downloadId);
        }
    }

    private ScheduledDownload SelectNext(List<ScheduledDownload> pending, DateTimeOffset now)
    {
        ScheduledDownload? best = null;
        var bestEffective = int.MinValue;
        foreach (var candidate in pending)
        {
            var effective = EffectivePriority(candidate, now);
            if (best is null ||
                effective > bestEffective ||
                (effective == bestEffective && candidate.SubmittedAt < best.SubmittedAt))
            {
                best = candidate;
                bestEffective = effective;
            }
        }

        return best ?? throw new InvalidOperationException("The pending queue is empty.");
    }

    private int EffectivePriority(ScheduledDownload download, DateTimeOffset now)
    {
        if (_agingBoost <= 0)
        {
            return download.Priority;
        }

        var waited = now - download.SubmittedAt;
        var steps = (int)(waited / _agingInterval);
        return download.Priority + steps * _agingBoost;
    }
}
