using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadResumeSegmentedTests
{
    private static readonly byte[] TotalContent = Enumerable.Range(0, 17).Select(value => (byte)(value + 1)).ToArray();
    private static readonly byte[] ConfirmedPrefix = TotalContent[..3];

    [TestMethod]
    public async Task ResumeSegmented_FromConfirmedOffset_DownloadsRemainingInSegmentsAndVerifies()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, ConfirmedPrefix);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 17);
        var orchestrator = CreateOrchestrator(ConfirmedPrefix, ConfirmedPrefix, source, writer, repository);

        var result = await orchestrator.ResumeSegmentedAsync(task, segmentCount: 4, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(17, task.ConfirmedBytes);
        Assert.AreEqual(4, source.OpenCount);
        CollectionAssert.AreEqual(TotalContent, writer.Bytes);
    }

    [TestMethod]
    public async Task ResumeSegmented_WithoutRangeSupport_FallsBackToSingleConnection()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, ConfirmedPrefix);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 17, supportsByteRanges: false);
        var orchestrator = CreateOrchestrator(
            ConfirmedPrefix,
            ConfirmedPrefix,
            source,
            writer,
            repository,
            supportsByteRanges: false);

        var result = await orchestrator.ResumeSegmentedAsync(task, segmentCount: 4, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(17, task.ConfirmedBytes);
        Assert.AreEqual(1, source.OpenCount);
        CollectionAssert.AreEqual(TotalContent, writer.Bytes);
    }

    [TestMethod]
    public async Task ResumeSegmented_WithUnknownLength_FallsBackToSingleConnection()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, ConfirmedPrefix);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: null);
        var orchestrator = CreateOrchestrator(
            ConfirmedPrefix,
            ConfirmedPrefix,
            source,
            writer,
            repository,
            remoteLength: null);

        var result = await orchestrator.ResumeSegmentedAsync(task, segmentCount: 4, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(1, source.OpenCount);
    }

    [TestMethod]
    public async Task ResumeSegmented_OverlapMismatch_ReturnsBlockedWithoutMutation()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, ConfirmedPrefix);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 17);
        var orchestrator = CreateOrchestrator(ConfirmedPrefix, [1, 9, 3], source, writer, repository);

        var result = await orchestrator.ResumeSegmentedAsync(task, segmentCount: 4, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.Blocked, result.Status);
        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapMismatched, result.Assessment.Status);
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(3, task.ConfirmedBytes);
        Assert.AreEqual(0, source.OpenCount);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task ResumeSegmented_AlreadyComplete_SkipsTransferAndMovesToVerification()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, TotalContent);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 17, remoteLength: 17);
        var orchestrator = CreateOrchestrator(TotalContent, TotalContent, source, writer, repository);

        var result = await orchestrator.ResumeSegmentedAsync(task, segmentCount: 4, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, result.Status);
        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(0, source.OpenCount);
        CollectionAssert.AreEqual(TotalContent, writer.Bytes);
    }

    [TestMethod]
    public async Task ResumeSegmented_RejectsNonPositiveSegmentCount()
    {
        var repository = new RecordingRepository();
        var writer = new PositionalMemoryWriter(TotalContent.Length, ConfirmedPrefix);
        var source = new OffsetAwareContentSource(TotalContent);
        var task = PreparedTask(confirmedBytes: 3, remoteLength: 17);
        var orchestrator = CreateOrchestrator(ConfirmedPrefix, ConfirmedPrefix, source, writer, repository);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.ResumeSegmentedAsync(task, segmentCount: 0, CancellationToken.None));
    }

    private static DownloadOrchestrator CreateOrchestrator(
        byte[] localContent,
        byte[] remoteOverlap,
        OffsetAwareContentSource contentSource,
        PositionalMemoryWriter writer,
        RecordingRepository repository,
        long? remoteLength = 17,
        bool supportsByteRanges = true)
    {
        var analyzer = new StubAnalyzer(remoteLength, supportsByteRanges);
        var recovery = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(new StubInspector(localContent.Length)),
            new RemoteIdentityReconciler(analyzer),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(
                new StubLocalReader(localContent),
                new StubRemoteReader(remoteOverlap)));
        return new DownloadOrchestrator(analyzer, contentSource, writer, repository, recovery);
    }

    private static DownloadTask PreparedTask(long confirmedBytes, long? remoteLength, bool supportsByteRanges = true) =>
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
                supportsByteRanges));

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

    private sealed class OffsetAwareContentSource(byte[] bytes, long? failingOffset = null) : IRemoteContentSource
    {
        private int _openCount;

        public int OpenCount => Volatile.Read(ref _openCount);

        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _openCount);
            if (failingOffset is { } failing && offset >= failing)
            {
                throw new IOException("Simulated segmented transfer failure.");
            }

            if (offset < 0 || offset > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(bytes, (int)offset, bytes.Length - (int)offset, writable: false), bytes.Length));
        }
    }

    private sealed class PositionalMemoryWriter : ITemporaryFileWriter
    {
        private readonly byte[] _buffer;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public PositionalMemoryWriter(int capacity, byte[] initialContent)
        {
            _buffer = new byte[capacity];
            initialContent.CopyTo(_buffer, 0);
        }

        public int WriteCount { get; private set; }

        public byte[] Bytes => (byte[])_buffer.Clone();

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
                WriteCount++;
                content.Span.CopyTo(_buffer.AsSpan((int)offset));
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
        public int SaveCount { get; private set; }

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult<DownloadTask?>(null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            SaveCount++;
            return ValueTask.CompletedTask;
        }
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
}
