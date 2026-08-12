using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Segmenting;

namespace WindowsDownloadManager.Application.Tests.Segmenting;

[TestClass]
public sealed class ChunkWorkQueueTests
{
    [TestMethod]
    public void Plan_CoversTheFullLengthWithoutGapsOrOverlaps()
    {
        const long total = 70_000;
        const int chunkSize = 7_000;
        var queue = new ChunkWorkQueue(total, chunkSize);

        Assert.AreEqual(10, queue.ChunkCount);

        var chunks = new List<DownloadChunk>();
        while (queue.TryAcquireNext() is { } chunk)
        {
            chunks.Add(chunk);
        }

        Assert.AreEqual(10, chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var expectedStart = (long)index * chunkSize;
            var expectedLength = Math.Min(chunkSize, total - expectedStart);
            Assert.AreEqual(expectedStart, chunk.StartOffset);
            Assert.AreEqual(expectedLength, chunk.Length);
            Assert.AreEqual(expectedStart + expectedLength, chunk.EndOffsetExclusive);
        }

        Assert.AreEqual(0, chunks[0].StartOffset);
        Assert.AreEqual(total, chunks[^1].EndOffsetExclusive);
    }

    [TestMethod]
    public void TryAcquireNext_WhenExhausted_ReturnsNull()
    {
        var queue = new ChunkWorkQueue(21, 7);

        Assert.IsNotNull(queue.TryAcquireNext());
        Assert.IsNotNull(queue.TryAcquireNext());
        Assert.IsNotNull(queue.TryAcquireNext());
        Assert.IsNull(queue.TryAcquireNext());
        Assert.IsNull(queue.TryAcquireNext());
    }

    [TestMethod]
    public void TryAcquireNext_WithTrailingPartialChunk_ReturnsBoundedTail()
    {
        var queue = new ChunkWorkQueue(22, 7);

        var first = queue.TryAcquireNext();
        var second = queue.TryAcquireNext();
        var third = queue.TryAcquireNext();
        var fourth = queue.TryAcquireNext();

        Assert.AreEqual(7, first!.Value.Length);
        Assert.AreEqual(7, second!.Value.Length);
        Assert.AreEqual(7, third!.Value.Length);
        Assert.AreEqual(1, fourth!.Value.Length);
        Assert.AreEqual(21, fourth.Value.StartOffset);
    }

    [TestMethod]
    public void SharedAcrossMultipleWorkers_EachChunkIsDistributedExactlyOnce()
    {
        const long total = 70_000;
        const int chunkSize = 7_000;
        const int workerCount = 4;
        var queue = new ChunkWorkQueue(total, chunkSize);

        var acquired = new ConcurrentBag<DownloadChunk>();
        var workers = new Task[workerCount];
        for (var index = 0; index < workerCount; index++)
        {
            workers[index] = Task.Run(() =>
            {
                while (queue.TryAcquireNext() is { } chunk)
                {
                    acquired.Add(chunk);
                }
            });
        }

        Task.WaitAll(workers);

        Assert.AreEqual(10, acquired.Count);

        var byStart = acquired
            .Select(chunk => chunk.StartOffset)
            .OrderBy(offset => offset)
            .ToArray();
        for (var index = 0; index < byStart.Length; index++)
        {
            Assert.AreEqual((long)index * chunkSize, byStart[index]);
        }

        Assert.AreEqual(total, acquired.Sum(chunk => chunk.Length));
    }

    [TestMethod]
    public void ComputeContiguousProgress_ReturnsLongestCompletedPrefix()
    {
        var queue = new ChunkWorkQueue(70_000, 7_000);

        var first = queue.TryAcquireNext()!.Value;
        var second = queue.TryAcquireNext()!.Value;
        var third = queue.TryAcquireNext()!.Value;

        Assert.AreEqual(0, queue.ComputeContiguousProgress());

        queue.MarkCompleted(first);
        Assert.AreEqual(7_000, queue.ComputeContiguousProgress());

        queue.MarkCompleted(third);
        Assert.AreEqual(7_000, queue.ComputeContiguousProgress());

        queue.MarkCompleted(second);
        Assert.AreEqual(21_000, queue.ComputeContiguousProgress());
    }

    [TestMethod]
    public void ComputeContiguousProgress_WhenAllCompleted_ReturnsTotalLength()
    {
        var queue = new ChunkWorkQueue(70_001, 7_000);

        var chunks = new List<DownloadChunk>();
        while (queue.TryAcquireNext() is { } chunk)
        {
            chunks.Add(chunk);
        }

        foreach (var chunk in chunks)
        {
            queue.MarkCompleted(chunk);
        }

        Assert.AreEqual(70_001L, queue.ComputeContiguousProgress());
    }

    [TestMethod]
    public void Ctor_RejectsInvalidArguments()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ChunkWorkQueue(-1, 1_000));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ChunkWorkQueue(1_000, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ChunkWorkQueue(1_000, -5));
    }
}
