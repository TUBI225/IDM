using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadResumeTests
{
    [TestMethod]
    public async Task Resume_MatchingCheckpoint_AppendsFromConfirmedOffsetBeforeSavingProgress()
    {
        var events = new List<string>();
        var repository = new RecordingRepository(events);
        var writer = new RecordingWriter(events);
        var contentSource = new RecordingContentSource([4, 5]);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 5);
        var orchestrator = CreateOrchestrator(
            localContent: [1, 2, 3],
            remoteOverlap: [1, 2, 3],
            contentSource,
            writer,
            repository);

        var result = await orchestrator.ResumeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
        Assert.AreEqual(3, contentSource.RequestedOffset);
        Assert.AreEqual(3, writer.RequestedOffset);
        CollectionAssert.AreEqual(new byte[] { 4, 5 }, writer.Written.ToArray());
        var flush = events.IndexOf("flush:5");
        var checkpoint = events.IndexOf("save:Downloading:5");
        Assert.IsGreaterThanOrEqualTo(0, flush);
        Assert.IsGreaterThan(flush, checkpoint);
    }

    [TestMethod]
    public async Task Resume_OverlapMismatch_ReturnsBlockedWithoutMutation()
    {
        var repository = new RecordingRepository([]);
        var writer = new RecordingWriter([]);
        var contentSource = new RecordingContentSource([4, 5]);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 5);
        var orchestrator = CreateOrchestrator(
            localContent: [1, 2, 3],
            remoteOverlap: [1, 9, 3],
            contentSource,
            writer,
            repository);

        var result = await orchestrator.ResumeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.Blocked, result.Status);
        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapMismatched, result.Assessment.Status);
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(3, task.ConfirmedBytes);
        Assert.AreEqual(0, contentSource.OpenCount);
        Assert.AreEqual(0, writer.WriteCount);
        Assert.AreEqual(0, repository.SaveCount);
    }

    [TestMethod]
    public async Task Resume_AlreadyComplete_SkipsBodyAndMovesToVerification()
    {
        var repository = new RecordingRepository([]);
        var writer = new RecordingWriter([]);
        var contentSource = new RecordingContentSource([]);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 3);
        var orchestrator = CreateOrchestrator(
            localContent: [1, 2, 3],
            remoteOverlap: [1, 2, 3],
            contentSource,
            writer,
            repository);

        var result = await orchestrator.ResumeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(0, contentSource.OpenCount);
        Assert.AreEqual(0, writer.WriteCount);
        Assert.AreEqual(1, repository.SaveCount);
    }

    private static DownloadOrchestrator CreateOrchestrator(
        byte[] localContent,
        byte[] remoteOverlap,
        RecordingContentSource contentSource,
        RecordingWriter writer,
        RecordingRepository repository)
    {
        var analyzer = new StubAnalyzer(localContent.Length + contentSource.ContentLength);
        var recovery = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(new StubInspector(localContent.Length)),
            new RemoteIdentityReconciler(analyzer),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(
                new StubLocalReader(localContent),
                new StubRemoteReader(remoteOverlap)));
        return new DownloadOrchestrator(analyzer, contentSource, writer, repository, recovery);
    }

    private static DownloadTask PreparedTask(long confirmedBytes, long remoteLength) =>
        DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Downloading,
            confirmedBytes,
            "C:\\Downloads\\file.download",
            new RemoteIdentity(
                new Uri("https://example.test/file.bin"),
                remoteLength,
                "\"v1\"",
                null,
                supportsByteRanges: true));

    private sealed class StubAnalyzer(long length) : IRemoteResourceAnalyzer
    {
        public ValueTask<RemoteResourceInfo> AnalyzeAsync(Uri uri, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new RemoteResourceInfo(uri, uri, length, null, null, "\"v1\"", null, true));
    }

    private sealed class StubInspector(long length) : ITemporaryFileInspector
    {
        public ValueTask<TemporaryFileSnapshot> InspectAsync(string temporaryPath, CancellationToken cancellationToken) =>
            ValueTask.FromResult(TemporaryFileSnapshot.Existing(length));
    }

    private sealed class StubLocalReader(byte[] content) : ITemporaryFileRangeReader
    {
        public ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
            string temporaryPath,
            long offset,
            int length,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TemporaryFileRangeSnapshot(content.Length, content));
    }

    private sealed class StubRemoteReader(byte[] content) : IRemoteRangeReader
    {
        public ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
            RemoteIdentity identity,
            long offset,
            int length,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<ReadOnlyMemory<byte>>(content);
    }

    private sealed class RecordingContentSource(byte[] content) : IRemoteContentSource
    {
        public int ContentLength => content.Length;
        public int OpenCount { get; private set; }
        public long? RequestedOffset { get; private set; }

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            OpenCount++;
            RequestedOffset = offset;
            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(content, writable: false), resource.Length));
        }
    }

    private sealed class RecordingWriter(List<string> events) : ITemporaryFileWriter
    {
        public MemoryStream Written { get; } = new();
        public int WriteCount { get; private set; }
        public long? RequestedOffset { get; private set; }

        public ValueTask PrepareNewAsync(string temporaryPath, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public async ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            WriteCount++;
            RequestedOffset = offset;
            await Written.WriteAsync(content, cancellationToken);
            events.Add($"flush:{offset + content.Length}");
            return offset + content.Length;
        }
    }

    private sealed class RecordingRepository(List<string> events) : IDownloadRepository
    {
        public int SaveCount { get; private set; }

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult<DownloadTask?>(null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            SaveCount++;
            events.Add($"save:{task.State}:{task.ConfirmedBytes}");
            return ValueTask.CompletedTask;
        }
    }
}
