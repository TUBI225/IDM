namespace WindowsDownloadManager.Application.Segmenting;

public readonly record struct DownloadChunk(long StartOffset, long Length)
{
    public long EndOffsetExclusive => checked(StartOffset + Length);
}

public sealed class ChunkWorkQueue
{
    private readonly long _totalLength;
    private readonly int _chunkSize;
    private readonly object _gate = new();
    private long _nextChunkStart;
    private readonly bool[] _completed;

    public ChunkWorkQueue(long totalLength, int chunkSize)
    {
        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        _totalLength = totalLength;
        _chunkSize = chunkSize;
        ChunkCount = (int)((totalLength + chunkSize - 1) / chunkSize);
        _completed = new bool[ChunkCount];
    }

    public int ChunkCount { get; }

    public DownloadChunk? TryAcquireNext()
    {
        lock (_gate)
        {
            if (_nextChunkStart >= _totalLength)
            {
                return null;
            }

            var start = _nextChunkStart;
            var length = (long)Math.Min(_chunkSize, _totalLength - start);
            _nextChunkStart = start + length;
            return new DownloadChunk(start, length);
        }
    }

    public void MarkCompleted(DownloadChunk chunk)
    {
        lock (_gate)
        {
            var index = (int)(chunk.StartOffset / _chunkSize);
            _completed[index] = true;
        }
    }

    public long ComputeContiguousProgress()
    {
        lock (_gate)
        {
            var progress = 0L;
            for (var index = 0; index < _completed.Length; index++)
            {
                if (!_completed[index])
                {
                    break;
                }

                progress = Math.Min(_totalLength, (index + 1) * (long)_chunkSize);
            }

            return progress;
        }
    }
}
