using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Host;

namespace WindowsDownloadManager.Host.Tests;

[TestClass]
public sealed class DownloadHostTests
{
    private static readonly byte[] Content = [1, 2, 3, 4, 5, 6, 7, 8];
    private const string Hash = "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824";
    private const string DestinationPath = "C:\\Downloads\\fixture.bin";

    [TestMethod]
    public async Task AddAndRunPending_NewDownload_CompletesAndFinalizes()
    {
        var repository = new StubRepository();
        var writer = new StubWriter();
        var contentSource = new StubContentSource(Content);
        var analyzer = new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true));
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content),
            new DownloadHostOptions(Connections: 1, Segments: 1, MaxConcurrentDownloads: 1));

        var task = await host.AddAsync(
            new Uri("https://example.test/fixture.bin"),
            DestinationPath,
            cancellationToken: CancellationToken.None);

        var count = await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(1, count);
        Assert.AreEqual(DownloadState.Completed, task.State);
        Assert.AreEqual(Content.Length, task.ConfirmedBytes);
        Assert.AreEqual(Content.Length, writer.Length);
        Assert.IsTrue(repository.Snapshots.Any(snapshot => snapshot.State == DownloadState.Completed));
    }

    [TestMethod]
    public async Task RunPending_DownloadingTask_ResumesAndFinalizes()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: 3);
        var contentSource = new StubContentSource(Content);
        var analyzer = new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true));
        var task = ResumedTask(confirmedBytes: 3, identity: Identity(Content.Length, supportsRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content.AsMemory(0, 3).ToArray()),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        var count = await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(1, count);
        Assert.AreEqual(DownloadState.Completed, task.State);
        Assert.AreEqual(Content.Length, task.ConfirmedBytes);
    }

    [TestMethod]
    public async Task RunPending_ResumeCapabilityLost_RetransmitsFromZeroAndFinalizes()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: 3);
        var contentSource = new StubContentSource(Content);
        var analyzer = new StubAnalyzer(
            Resource(uri: null, length: Content.Length, supportsRanges: false));
        var task = ResumedTask(confirmedBytes: 3, identity: Identity(Content.Length, supportsRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content.AsMemory(0, 3).ToArray()),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        var count = await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(1, count);
        Assert.AreEqual(DownloadState.Completed, task.State);
        Assert.AreEqual(Content.Length, task.ConfirmedBytes);
        Assert.AreEqual(0, contentSource.RequestedOffset);
    }

    [TestMethod]
    public async Task RunPending_IdentityContradicted_StopsSafelyAsRemoteFileChanged()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: 3);
        var contentSource = new StubContentSource(Content);
        var analyzer = new StubAnalyzer(
            new RemoteResourceInfo(
                new Uri("https://example.test/fixture.bin"),
                new Uri("https://example.test/fixture.bin"),
                Content.Length,
                SuggestedFileName: null,
                ContentType: null,
                EntityTag: "\"changed\"",
                LastModified: null,
                SupportsByteRanges: true));
        var task = ResumedTask(
            confirmedBytes: 3,
            identity: Identity(Content.Length, supportsRanges: true, entityTag: "\"v1\""));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(DownloadState.RemoteFileChanged, task.State);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task RunPending_VerifyingTask_FinalizesToCompleted()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: Content.Length);
        var contentSource = new StubContentSource(Content);
        var task = TaskIn(
            DownloadState.Verifying,
            confirmedBytes: Content.Length,
            temporaryPath: TemporaryPath,
            identity: Identity(Content.Length, supportsRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true)),
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
    }

    [TestMethod]
    public async Task RunPending_FinalizingTask_RepairsToCompleted()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: Content.Length);
        var contentSource = new StubContentSource(Content);
        var task = TaskIn(
            DownloadState.Finalizing,
            confirmedBytes: Content.Length,
            temporaryPath: TemporaryPath,
            identity: Identity(Content.Length, supportsRanges: true),
            verifiedSha256: Hash);
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true)),
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        await host.RunPendingAsync(CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
    }

    [TestMethod]
    public async Task CancelAsync_TransitionsToCancelled()
    {
        var repository = new StubRepository();
        var task = TaskIn(DownloadState.Waiting, confirmedBytes: 0, temporaryPath: null, identity: null);
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            new StubWriter(),
            new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true)),
            new StubContentSource(Content),
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content));

        await host.CancelAsync(task.Id, CancellationToken.None);

        Assert.AreEqual(DownloadState.Cancelled, task.State);
    }

    [TestMethod]
    public async Task PauseAsync_TransitionsThroughPauseRequestedToPaused()
    {
        var repository = new StubRepository();
        var task = TaskIn(
            DownloadState.Downloading,
            confirmedBytes: 2,
            temporaryPath: TemporaryPath,
            identity: Identity(Content.Length, supportsRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            new StubWriter(),
            new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true)),
            new StubContentSource(Content),
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content));

        await host.PauseAsync(task.Id, CancellationToken.None);

        Assert.AreEqual(DownloadState.Paused, task.State);
        Assert.IsTrue(repository.Snapshots.Any(snapshot => snapshot.State == DownloadState.PauseRequested));
        Assert.IsTrue(repository.Snapshots.Any(snapshot => snapshot.State == DownloadState.Paused));
    }

    [TestMethod]
    public async Task CancelAsync_WhileTransferring_StopsTheRunningDownloadAndPersistsCancelled()
    {
        var repository = new StubRepository();
        var writer = new StubWriter(initialLength: 3);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contentSource = new StubBlockingContentSource(Content, release);
        var analyzer = new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true));
        var task = ResumedTask(confirmedBytes: 3, identity: Identity(Content.Length, supportsRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content.AsMemory(0, 3).ToArray()),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        var run = host.RunPendingAsync(CancellationToken.None).AsTask();
        await contentSource.Opened.Task;
        await host.CancelAsync(task.Id, CancellationToken.None);

        release.SetResult();
        var count = await run;

        Assert.AreEqual(DownloadState.Cancelled, task.State);
        Assert.AreEqual(1, count);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task IpcCommandServer_HandlesCancelCommand_InvokesHandlerAndRespondsOk()
    {
        var invoked = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new IpcCommandServer(
            cancelHandler: (id, token) =>
            {
                invoked.SetResult(id);
                return ValueTask.CompletedTask;
            },
            pauseHandler: (id, token) => ValueTask.CompletedTask);

        var id = Guid.NewGuid();
        var ok = await IpcCommandClient.TrySendAsync(
            "CANCEL",
            id,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.IsTrue(ok);
        Assert.AreEqual(id, await invoked.Task);
    }

    [TestMethod]
    public async Task IpcCommandServer_HandlesPauseCommand_InvokesHandlerAndRespondsOk()
    {
        var invoked = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new IpcCommandServer(
            cancelHandler: (id, token) => ValueTask.CompletedTask,
            pauseHandler: (id, token) =>
            {
                invoked.SetResult(id);
                return ValueTask.CompletedTask;
            });

        var id = Guid.NewGuid();
        var ok = await IpcCommandClient.TrySendAsync(
            "PAUSE",
            id,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.IsTrue(ok);
        Assert.AreEqual(id, await invoked.Task);
    }

    [TestMethod]
    public async Task RunPending_HigherPriorityDownloadRunsFirst()
    {
        var repository = new StubRepository();
        var writer = new StubWriter();
        var contentSource = new StubContentSource(Content);
        var analyzer = new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true));
        var host = CreateHost(
            repository,
            writer,
            analyzer,
            contentSource,
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content),
            new DownloadHostOptions(Connections: 1, Segments: 1));

        var low = await host.AddAsync(
            new Uri("https://example.test/low.bin"),
            DestinationPath,
            priority: 0,
            cancellationToken: CancellationToken.None);
        var high = await host.AddAsync(
            new Uri("https://example.test/high.bin"),
            DestinationPath,
            priority: 10,
            cancellationToken: CancellationToken.None);

        var first = await host.RunOnceAsync(CancellationToken.None);

        Assert.IsNotNull(first);
        Assert.AreEqual(high.Id, first.Id);
        Assert.AreEqual(DownloadState.Completed, high.State);
        Assert.AreEqual(DownloadState.New, low.State);
    }

    [TestMethod]
    public async Task AddAsync_InvalidArguments_Throw()
    {
        var repository = new StubRepository();
        var host = CreateHost(
            repository,
            new StubWriter(),
            new StubAnalyzer(Resource(uri: null, length: Content.Length, supportsRanges: true)),
            new StubContentSource(Content),
            new StubRangeReader(Content),
            new StubLocalRangeReader(Content));

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => host
            .AddAsync(null!, DestinationPath, cancellationToken: CancellationToken.None)
            .AsTask());
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => host
            .AddAsync(new Uri("https://example.test/f.bin"), " ", cancellationToken: CancellationToken.None)
            .AsTask());
    }


    private const string TemporaryPath = "C:\\Downloads\\.fixture.bin.wdm-partial";

    private static DownloadHost CreateHost(
        StubRepository repository,
        StubWriter writer,
        StubAnalyzer analyzer,
        IRemoteContentSource contentSource,
        StubRangeReader rangeReader,
        StubLocalRangeReader localRangeReader,
        DownloadHostOptions? options = null)
    {
        var services = new DownloadHostServices(
            analyzer,
            contentSource,
            writer,
            new StubInspector(() => writer.Length),
            localRangeReader,
            rangeReader,
            new StubHasher(),
            new StubFinalizer(),
            repository);
        return new DownloadHost(services, options);
    }

    private static RemoteResourceInfo Resource(Uri? uri, long? length, bool supportsRanges) =>
        new(
            uri ?? new Uri("https://example.test/fixture.bin"),
            uri ?? new Uri("https://example.test/fixture.bin"),
            length,
            SuggestedFileName: null,
            ContentType: null,
            EntityTag: "\"v1\"",
            LastModified: null,
            supportsRanges);

    private static RemoteIdentity Identity(
        long? length,
        bool supportsRanges,
        string? entityTag = "\"v1\"") =>
        new(
            new Uri("https://example.test/fixture.bin"),
            length,
            entityTag,
            lastModified: null,
            supportsRanges);

    private static DownloadTask ResumedTask(long confirmedBytes, RemoteIdentity identity) =>
        DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/fixture.bin"),
            DestinationPath,
            DownloadState.Downloading,
            confirmedBytes,
            TemporaryPath,
            identity,
            verifiedSha256: null);

    private static DownloadTask TaskIn(
        DownloadState state,
        long confirmedBytes,
        string? temporaryPath,
        RemoteIdentity? identity,
        string? verifiedSha256 = null) =>
        DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/fixture.bin"),
            DestinationPath,
            state,
            confirmedBytes,
            temporaryPath,
            identity,
            verifiedSha256);


    private sealed class StubAnalyzer(RemoteResourceInfo resource) : IRemoteResourceAnalyzer
    {
        public ValueTask<RemoteResourceInfo> AnalyzeAsync(Uri uri, CancellationToken cancellationToken) =>
            ValueTask.FromResult(resource);
    }

    private sealed class StubContentSource(byte[] content) : IRemoteContentSource
    {
        public int OpenCount { get; private set; }
        public long? RequestedOffset { get; private set; }

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            OpenCount++;
            RequestedOffset = offset;
            var start = (int)Math.Min(offset, content.Length);
            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(content.AsMemory(start).ToArray(), writable: false), resource.Length));
        }
    }

    private sealed class StubRangeReader(byte[] content) : IRemoteRangeReader
    {
        public ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
            RemoteIdentity identity,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            var start = (int)Math.Min(offset, content.Length);
            var count = (int)Math.Min(length, content.Length - start);
            return ValueTask.FromResult<ReadOnlyMemory<byte>>(content.AsMemory(start, count));
        }
    }

    private sealed class StubLocalRangeReader(byte[] content) : ITemporaryFileRangeReader
    {
        public ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
            string temporaryPath,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            var start = (int)Math.Min(offset, content.Length);
            var count = (int)Math.Min(length, content.Length - start);
            return ValueTask.FromResult(
                new TemporaryFileRangeSnapshot(content.Length, content.AsMemory(start, count)));
        }
    }

    private sealed class StubWriter : ITemporaryFileWriter
    {
        private readonly Dictionary<long, byte[]> _blocks = [];

        public StubWriter(long initialLength = 0)
        {
            Length = initialLength;
        }

        public int WriteCount { get; private set; }
        public long Length { get; private set; }

        public ValueTask PrepareNewAsync(string temporaryPath, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            WriteCount++;
            _blocks[offset] = content.ToArray();
            Length = Math.Max(Length, offset + content.Length);
            return ValueTask.FromResult(offset + content.Length);
        }
    }

    private sealed class StubInspector(Func<long> lengthProvider) : ITemporaryFileInspector
    {
        public ValueTask<TemporaryFileSnapshot> InspectAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (path.EndsWith(".wdm-partial", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult(TemporaryFileSnapshot.Existing(lengthProvider()));
            }

            return ValueTask.FromResult(TemporaryFileSnapshot.Absent);
        }
    }

    private sealed class StubHasher : ITemporaryFileHasher
    {
        public ValueTask<string> ComputeSha256Async(string path, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Hash);
    }

    private sealed class StubFinalizer : ITemporaryFileFinalizer
    {
        public ValueTask FinalizeAsync(
            Guid downloadId,
            string temporaryPath,
            string destinationPath,
            string verifiedSha256,
            CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask RepairAsync(
            Guid downloadId,
            string temporaryPath,
            string destinationPath,
            string verifiedSha256,
            CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private sealed class StubRepository : IDownloadRepository
    {
        private readonly Dictionary<Guid, DownloadTask> _tasks = [];

        public List<RepositorySnapshot> Snapshots { get; } = [];

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult(_tasks.TryGetValue(id, out var task) ? task : null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            _tasks[task.Id] = task;
            Snapshots.Add(new RepositorySnapshot(task.State, task.ConfirmedBytes));
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<DownloadTask>> ListNonTerminalAsync(
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<DownloadTask>>(
                _tasks.Values
                    .Where(task => task.State is not (
                        DownloadState.Completed or DownloadState.Cancelled))
                    .ToArray());
    }

    private sealed record RepositorySnapshot(DownloadState State, long ConfirmedBytes);

    private sealed class StubBlockingContentSource(byte[] content, TaskCompletionSource release) : IRemoteContentSource
    {
        public TaskCompletionSource Opened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            Opened.TrySetResult();
            var start = (int)Math.Min(offset, content.Length);
            return ValueTask.FromResult<RemoteContentLease>(
                new(new BlockingReadStream(content.AsMemory(start).ToArray(), release), resource.Length));
        }
    }

    private sealed class BlockingReadStream(byte[] content, TaskCompletionSource release) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => content.Length;
        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            var bytes = Math.Min(count, content.Length);
            if (bytes > 0)
            {
                Array.Copy(content, 0, buffer, offset, bytes);
            }

            return bytes;
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}

