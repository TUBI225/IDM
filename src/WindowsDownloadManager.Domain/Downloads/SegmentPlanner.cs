namespace WindowsDownloadManager.Domain.Downloads;

public sealed record DownloadSegment(long StartOffset, long Length)
{
    public long EndOffsetExclusive => checked(StartOffset + Length);
}

public static class SegmentPlanner
{
    public static IReadOnlyList<DownloadSegment> Plan(long totalLength, int segmentCount)
    {
        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        if (segmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

        if (totalLength == 0)
        {
            return Array.Empty<DownloadSegment>();
        }

        var effectiveCount = (int)Math.Min(segmentCount, totalLength);
        if (effectiveCount == 1)
        {
            return [new DownloadSegment(0, totalLength)];
        }

        var baseLength = totalLength / effectiveCount;
        var remainder = totalLength % effectiveCount;
        var segments = new List<DownloadSegment>(effectiveCount);
        var offset = 0L;
        for (var index = 0; index < effectiveCount; index++)
        {
            var length = baseLength + (index < remainder ? 1 : 0);
            segments.Add(new DownloadSegment(offset, length));
            offset = checked(offset + length);
        }

        Validate(segments, totalLength);
        return segments;
    }

    public static void Validate(IReadOnlyList<DownloadSegment> segments, long totalLength)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        var expectedOffset = 0L;
        foreach (var segment in segments)
        {
            if (segment.Length <= 0)
            {
                throw new ArgumentException("A segment must have a positive length.", nameof(segments));
            }

            if (segment.StartOffset != expectedOffset)
            {
                throw new ArgumentException("The segments must be contiguous and ordered.", nameof(segments));
            }

            expectedOffset = checked(segment.StartOffset + segment.Length);
        }

        if (expectedOffset != totalLength)
        {
            throw new ArgumentException("The segments must cover the total length exactly.", nameof(segments));
        }
    }
}
