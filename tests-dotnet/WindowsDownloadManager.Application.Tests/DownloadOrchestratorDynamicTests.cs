using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadOrchestratorDynamicTests
{
    private static readonly byte[] Content = Enumerable.Range(0, 70_000).Select(value => (byte)(value % 251)).ToArray();
    private const int ChunkSize = 7_000;

    [TestMethod]
    public async Task RunDynamic_WithRangeSupport_AssemblesEveryChunkExactly()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        var result = await orchestrator.RunDynamicSegmentedAsync(
            task,
            "C:\\\\Downloads\\\\fixture.download",
            connectionCount: 3,
            chunkSize: ChunkSize,
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(Content.Length, result.ConfirmedBytes);
        Assert.AreEqual(10, source.BoundedOpenCount);
        CollectionAssert.AreEqual(Content, writer.Bytes);
        Assert.IsTrue(repository.Snapshots.Any(snapshot => snapshot.ConfirmedBytes == Content.Length));
    }

    [TestMethod]
    public async Task RunDynamic_WithSingleConnection_FallsBackToWholeFile()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        var result = await orchestrator.RunDynamicSegmentedAsync(
            task,
            "C:\\\\Downloads\\\\fixture.download",
            connectionCount: 1,
            chunkSize: ChunkSize,
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(Content.Length, result.ConfirmedBytes);
        Assert.AreEqual(1, source.OpenCount);
        Assert.AreEqual(0, source.BoundedOpenCount);
        CollectionAssert.AreEqual(Content, writer.Bytes);
    }

    [TestMethod]
    public async Task RunDynamic_WithoutRangeSupport_FallsBackToSingleConnection()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository, supportsByteRanges: false);
        var task = NewTask();

        var result = await orchestrator.RunDynamicSegmentedAsync(
            task,
            "C:\\\\Downloads\\\\fixture.download",
            connectionCount: 4,
            chunkSize: ChunkSize,
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(Content.Length, result.ConfirmedBytes);
        Assert.AreEqual(1, source.OpenCount);
        CollectionAssert.AreEqual(Content, writer.Bytes);
    }

    [TestMethod]
    public async Task RunDynamic_WithUnknownLength_FallsBackToSingleConnection()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(
            null,
            source,
            writer,
            repository,
            supportsByteRanges: true);
        var task = NewTask();

        var result = await orchestrator.RunDynamicSegmentedAsync(
            task,
            "C:\\\\Downloads\\\\fixture.download",
            connectionCount: 4,
            chunkSize: ChunkSize,
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(Content.Length, result.ConfirmedBytes);
        Assert.AreEqual(1, source.OpenCount);
        CollectionAssert.AreEqual(Content, writer.Bytes);
    }

    [TestMethod]
    public async Task RunDynamic_ZeroLength_PreparesWithoutNetworkBody()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Array.Empty<byte>());
        var orchestrator = CreateOrchestrator(0, source, writer, repository);
        var task = NewTask();

        var result = await orchestrator.RunDynamicSegmentedAsync(
            task,
            "C:\\\\Downloads\\\\empty.download",
            connectionCount: 4,
            chunkSize: ChunkSize,
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(0, source.OpenCount);
        Assert.AreEqual(0, source.BoundedOpenCount);
    }

    [TestMethod]
    public async Task RunDynamic_WhenFirstChunkFails_KeepsZeroContiguousProgress()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content, failingOffset: 0);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await orchestrator.RunDynamicSegmentedAsync(
                task,
                "C:\\\\Downloads\\\\fixture.download",
                connectionCount: 3,
                chunkSize: ChunkSize,
                CancellationToken.None));

        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(0, task.ConfirmedBytes);
    }

    [TestMethod]
    public async Task RunDynamic_WhenLaterChunksFail_KeepsTheCompletedPrefix()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content, failingOffset: 35_000);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await orchestrator.RunDynamicSegmentedAsync(
                task,
                "C:\\\\Downloads\\\\fixture.download",
                connectionCount: 3,
                chunkSize: ChunkSize,
                CancellationToken.None));

        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.IsTrue(task.ConfirmedBytes > 0 && task.ConfirmedBytes < Content.Length);

        var confirmed = (int)task.ConfirmedBytes;
        var prefix = writer.Bytes.Take(confirmed).ToArray();
        CollectionAssert.AreEqual(Content.Take(confirmed).ToArray(), prefix);
        Assert.IsTrue(repository.Snapshots.Any(snapshot => snapshot.ConfirmedBytes == task.ConfirmedBytes));
    }

    [TestMethod]
    public async Task RunDynamic_RejectsInvalidChunkSize()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.RunDynamicSegmentedAsync(
                task,
                "C:\\\\Downloads\\\\fixture.download",
                connectionCount: 4,
                chunkSize: 0,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task RunDynamic_RejectsInvalidConnectionCount()
    {
        var repository = new RecordingRepository();
        var writer = new ThreadSafeMemoryWriter();
        var source = new OffsetAwareContentSource(Content);
        var orchestrator = CreateOrchestrator(Content.Length, source, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.RunDynamicSegmentedAsync(
                task,
                "C:\\\\Downloads\\\\fixture.download",
                connectionCount: 0,
                chunkSize: ChunkSize,
                CancellationToken.None));
    }

    private static DownloadOrchestrator CreateOrchestrator(
        long? length,
        IRemoteContentSource source,
        ITemporaryFileWriter writer,
        IDownloadRepository repository,
        bool supportsByteRanges = true) =>
        new(new StubAnalyzer(length, supportsByteRanges), source, writer, repository);

    private static DownloadTask NewTask() => new(
        Guid.NewGuid(),
        new Uri("https://example.test/fixture.bin"),
        "C:\\\\Downloads\\\\fixture.bin");

    private sealed class StubAnalyzer(long? length, bool supportsByteRanges) : IRemoteResourceAnalyzer
    {
        public ValueTask<RemoteResourceInfo> AnalyzeAsync(Uri uri, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new RemoteResourceInfo(
                uri,
                uri,
                length,
                null,
                null,
                "\"v1\"",
                null,
                supportsByteRanges));
    }

    private sealed class OffsetAwareContentSource(byte[] bytes, long? failingOffset = null)
        : IRemoteContentSource, IRemoteBoundedContentSource
    {
        private int _openCount;
        private int _boundedOpenCount;

        public int OpenCount => Volatile.Read(ref _openCount);
        public int BoundedOpenCount => Volatile.Read(ref _boundedOpenCount);

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _openCount);
            if (failingOffset is { } failing && offset >= failing)
            {
                throw new IOException("Simulated dynamic transfer failure.");
            }

            if (offset < 0 || offset > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(bytes, (int)offset, bytes.Length - (int)offset, writable: false), bytes.Length));
        }

        public ValueTask<RemoteContentLease> OpenBoundedReadAsync(
            RemoteResourceInfo resource,
            long start,
            long end,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _openCount);
            Interlocked.Increment(ref _boundedOpenCount);
            if (failingOffset is { } failing && start >= failing)
            {
                throw new IOException("Simulated dynamic transfer failure.");
            }

            if (start < 0 || end < start || end >= bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(bytes, (int)start, (int)(end - start + 1), writable: false), bytes.Length));
        }
    }

    private sealed class ThreadSafeMemoryWriter : ITemporaryFileWriter
    {
        private readonly MemoryStream _bytes = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        public byte[] Bytes => _bytes.ToArray();

        public ValueTask PrepareNewAsync(string temporaryPath, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public async ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _bytes.Position = offset;
                await _bytes.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                return offset + content.Length;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    private sealed class RecordingRepository : IDownloadRepository
    {
        private readonly List<(DownloadState State, long ConfirmedBytes)> _snapshots = [];
        private readonly object _gate = new();

        public IReadOnlyList<(DownloadState State, long ConfirmedBytes)> Snapshots
        {
            get
            {
                lock (_gate)
                {
                    return _snapshots.ToArray();
                }
            }
        }

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult<DownloadTask?>(null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _snapshots.Add((task.State, task.ConfirmedBytes));
            }

            return ValueTask.CompletedTask;
        }
    }
}

