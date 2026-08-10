using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadOrchestratorTests
{
    [TestMethod]
    public async Task RunNew_FlushesEveryBlockBeforeConfirmingIt()
    {
        var bytes = Enumerable.Range(0, 70_000).Select(value => (byte)(value % 251)).ToArray();
        var events = new List<string>();
        var repository = new RecordingRepository(events);
        var writer = new RecordingWriter(events);
        var orchestrator = CreateOrchestrator(bytes, writer, repository);
        var task = NewTask();

        var result = await orchestrator.RunNewAsync(
            task,
            "C:\\Downloads\\fixture.download",
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.AreEqual(bytes.Length, result.ConfirmedBytes);
        Assert.AreEqual("C:\\Downloads\\fixture.download", task.TemporaryPath);
        Assert.IsNotNull(task.RemoteIdentity);
        Assert.AreEqual(bytes.Length, task.RemoteIdentity.Length);
        CollectionAssert.AreEqual(bytes, writer.Bytes.ToArray());
        var firstFlush = events.FindIndex(item => item.StartsWith("flush:", StringComparison.Ordinal));
        var firstPositiveSave = events.FindIndex(item => item.StartsWith("save:Downloading:", StringComparison.Ordinal) && !item.EndsWith(":0", StringComparison.Ordinal));
        Assert.IsGreaterThanOrEqualTo(0, firstFlush);
        Assert.IsGreaterThan(firstFlush, firstPositiveSave);
    }

    [TestMethod]
    public async Task RunNew_WhenFlushFails_DoesNotConfirmUnflushedBytes()
    {
        var repository = new RecordingRepository([]);
        var writer = new RecordingWriter([], failWrite: true);
        var orchestrator = CreateOrchestrator(new byte[] { 1, 2, 3 }, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await orchestrator.RunNewAsync(task, "C:\\Downloads\\fixture.download", CancellationToken.None));

        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(0, task.ConfirmedBytes);
        Assert.IsFalse(repository.Snapshots.Any(snapshot => snapshot.ConfirmedBytes > 0));
    }

    [TestMethod]
    public async Task RunNew_WhenPreparationCheckpointFails_DoesNotCreateTemporaryFile()
    {
        var repository = new RecordingRepository([], failOnState: DownloadState.Preparing);
        var writer = new RecordingWriter([]);
        var orchestrator = CreateOrchestrator(new byte[] { 1, 2, 3 }, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await orchestrator.RunNewAsync(task, "C:\\Downloads\\fixture.download", CancellationToken.None));

        Assert.AreEqual(DownloadState.Preparing, task.State);
        Assert.IsNotNull(task.RemoteIdentity);
        Assert.IsFalse(writer.Prepared);
    }

    [TestMethod]
    public async Task RunNew_PrematureEnd_IsRejectedAndRemainsRecoverable()
    {
        var repository = new RecordingRepository([]);
        var writer = new RecordingWriter([]);
        var analyzer = new StubAnalyzer(length: 4);
        var source = new StubContentSource(new byte[] { 1, 2, 3 }, totalLength: 4);
        var orchestrator = new DownloadOrchestrator(analyzer, source, writer, repository);
        var task = NewTask();

        await Assert.ThrowsExactlyAsync<EndOfStreamException>(async () =>
            await orchestrator.RunNewAsync(task, "C:\\Downloads\\fixture.download", CancellationToken.None));

        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(3, task.ConfirmedBytes);
        Assert.AreEqual(3, repository.Snapshots[^1].ConfirmedBytes);
    }

    [TestMethod]
    public async Task RunNew_ZeroLength_PreparesFileWithoutOpeningNetworkBody()
    {
        var repository = new RecordingRepository([]);
        var writer = new RecordingWriter([]);
        var source = new StubContentSource(Array.Empty<byte>(), 0);
        var orchestrator = new DownloadOrchestrator(new StubAnalyzer(0), source, writer, repository);
        var task = NewTask();

        var result = await orchestrator.RunNewAsync(
            task,
            "C:\\Downloads\\empty.download",
            CancellationToken.None);

        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.IsTrue(writer.Prepared);
        Assert.AreEqual(0, source.OpenCount);
    }

    private static DownloadOrchestrator CreateOrchestrator(
        byte[] bytes,
        RecordingWriter writer,
        RecordingRepository repository) =>
        new(new StubAnalyzer(bytes.Length), new StubContentSource(bytes, bytes.Length), writer, repository);

    private static DownloadTask NewTask() => new(
        Guid.NewGuid(),
        new Uri("https://example.test/fixture.bin"),
        "C:\\Downloads\\fixture.bin");

    private sealed class StubAnalyzer(long length) : IRemoteResourceAnalyzer
    {
        public ValueTask<RemoteResourceInfo> AnalyzeAsync(Uri uri, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new RemoteResourceInfo(uri, uri, length, null, null, "\"v1\"", null, true));
    }

    private sealed class StubContentSource(byte[] bytes, long? totalLength) : IRemoteContentSource
    {
        public int OpenCount { get; private set; }

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            OpenCount++;
            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(bytes, writable: false), totalLength));
        }
    }

    private sealed class RecordingWriter(List<string> events, bool failWrite = false) : ITemporaryFileWriter
    {
        public MemoryStream Bytes { get; } = new();
        public bool Prepared { get; private set; }

        public ValueTask PrepareNewAsync(string temporaryPath, CancellationToken cancellationToken)
        {
            Prepared = true;
            events.Add("prepare");
            return ValueTask.CompletedTask;
        }

        public async ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            if (failWrite)
            {
                throw new IOException("Simulated durable write failure.");
            }

            Bytes.Position = offset;
            await Bytes.WriteAsync(content, cancellationToken);
            events.Add($"flush:{offset + content.Length}");
            return offset + content.Length;
        }
    }

    private sealed class RecordingRepository(
        List<string> events,
        DownloadState? failOnState = null) : IDownloadRepository
    {
        public List<(DownloadState State, long ConfirmedBytes, string? TemporaryPath, RemoteIdentity? Identity)> Snapshots { get; } = [];

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult<DownloadTask?>(null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            if (task.State == failOnState)
            {
                throw new IOException("Simulated preparation checkpoint failure.");
            }

            Snapshots.Add((task.State, task.ConfirmedBytes, task.TemporaryPath, task.RemoteIdentity));
            events.Add($"save:{task.State}:{task.ConfirmedBytes}");
            return ValueTask.CompletedTask;
        }
    }
}
