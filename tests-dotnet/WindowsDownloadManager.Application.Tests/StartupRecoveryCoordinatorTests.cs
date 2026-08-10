using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class StartupRecoveryCoordinatorTests
{
    [TestMethod]
    public async Task Coordinate_RecoveryMetadataAbsent_ShortCircuitsBeforeDiskAndNetwork()
    {
        var fixture = new Fixture(TemporaryFileSnapshot.Existing(5), [1, 2, 3, 4, 5]);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Downloading,
            confirmedBytes: 5);

        var result = await fixture.Coordinator.CoordinateAsync(task, CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.BlockedBeforeRemoteAnalysis, result.Status);
        Assert.AreEqual(RecoveryBlocker.RecoveryMetadataAbsent, result.ReconciliationBlockers);
        Assert.IsNull(result.RemoteIdentity);
        Assert.IsNull(result.Decision);
        Assert.IsNull(result.Overlap);
        Assert.AreEqual(0, fixture.Inspector.InspectionCount);
        Assert.AreEqual(0, fixture.Analyzer.AnalysisCount);
        Assert.AreEqual(0, fixture.LocalReader.ReadCount);
        Assert.AreEqual(0, fixture.RemoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Coordinate_TemporaryFileShorter_ShortCircuitsBeforeNetwork()
    {
        var fixture = new Fixture(TemporaryFileSnapshot.Existing(4), [1, 2, 3, 4]);

        var result = await fixture.Coordinator.CoordinateAsync(PreparedTask(), CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.BlockedBeforeRemoteAnalysis, result.Status);
        Assert.AreEqual(RecoveryBlocker.CheckpointAheadOfTemporaryFile, result.ReconciliationBlockers);
        Assert.AreEqual(1, fixture.Inspector.InspectionCount);
        Assert.AreEqual(0, fixture.Analyzer.AnalysisCount);
        Assert.AreEqual(0, fixture.LocalReader.ReadCount);
        Assert.AreEqual(0, fixture.RemoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Coordinate_RemoteContradiction_StopsBeforeOverlap()
    {
        var fixture = new Fixture(
            TemporaryFileSnapshot.Existing(5),
            [1, 2, 3, 4, 5],
            observedLength: 6);

        var result = await fixture.Coordinator.CoordinateAsync(PreparedTask(), CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.BlockedAfterRemoteAnalysis, result.Status);
        Assert.AreEqual(RecoveryBlocker.RemoteIdentityContradictory, result.ReconciliationBlockers);
        Assert.IsNotNull(result.RemoteIdentity);
        Assert.IsNotNull(result.Decision);
        Assert.IsNull(result.Overlap);
        Assert.AreEqual(1, fixture.Analyzer.AnalysisCount);
        Assert.AreEqual(0, fixture.LocalReader.ReadCount);
        Assert.AreEqual(0, fixture.RemoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Coordinate_MatchingEvidence_ExecutesFullReadOnlySequence()
    {
        byte[] content = [1, 2, 3, 4, 5];
        var fixture = new Fixture(TemporaryFileSnapshot.Existing(5), content);
        var task = PreparedTask();

        var result = await fixture.Coordinator.CoordinateAsync(task, CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapMatched, result.Status);
        Assert.AreEqual(RecoveryBlocker.None, result.ReconciliationBlockers);
        Assert.IsNotNull(result.RemoteIdentity);
        Assert.IsNotNull(result.Decision);
        Assert.IsNotNull(result.Overlap);
        Assert.AreEqual(OverlapVerificationStatus.Match, result.Overlap.Status);
        Assert.AreEqual(1, fixture.Inspector.InspectionCount);
        Assert.AreEqual(1, fixture.Analyzer.AnalysisCount);
        Assert.AreEqual(1, fixture.LocalReader.ReadCount);
        Assert.AreEqual(1, fixture.RemoteReader.ReadCount);
        AssertTaskUnchanged(task);
    }

    [TestMethod]
    public async Task Coordinate_DifferentOverlap_ReturnsTypedMismatch()
    {
        var fixture = new Fixture(
            TemporaryFileSnapshot.Existing(5),
            [1, 2, 3, 4, 5],
            remoteContent: [1, 2, 9, 4, 5]);

        var result = await fixture.Coordinator.CoordinateAsync(PreparedTask(), CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapMismatched, result.Status);
        Assert.IsNotNull(result.Overlap);
        Assert.AreEqual(OverlapVerificationStatus.Mismatch, result.Overlap.Status);
    }

    [TestMethod]
    public async Task Coordinate_FileChangedDuringOverlap_StopsBeforeRemoteRangeRead()
    {
        var fixture = new Fixture(
            TemporaryFileSnapshot.Existing(5),
            localContent: [],
            localRangeFileLength: 4);

        var result = await fixture.Coordinator.CoordinateAsync(PreparedTask(), CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.LocalFileChangedDuringOverlap, result.Status);
        Assert.IsNotNull(result.Overlap);
        Assert.AreEqual(OverlapVerificationStatus.LocalFileChanged, result.Overlap.Status);
        Assert.AreEqual(1, fixture.LocalReader.ReadCount);
        Assert.AreEqual(0, fixture.RemoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Coordinate_ZeroCheckpoint_DoesNotReadOverlap()
    {
        var fixture = new Fixture(
            TemporaryFileSnapshot.Existing(0),
            [],
            observedLength: 0);

        var result = await fixture.Coordinator.CoordinateAsync(
            PreparedTask(confirmedBytes: 0, remoteLength: 0),
            CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapNotRequired, result.Status);
        Assert.IsNotNull(result.Overlap);
        Assert.AreEqual(OverlapVerificationStatus.NotRequired, result.Overlap.Status);
        Assert.AreEqual(0, fixture.LocalReader.ReadCount);
        Assert.AreEqual(0, fixture.RemoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Coordinate_CancelledAfterLocalInspection_StopsBeforeNetwork()
    {
        using var cancellation = new CancellationTokenSource();
        var inspector = new CancellingInspector(cancellation);
        var analyzer = new StubAnalyzer(observedLength: 5);
        var coordinator = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(inspector),
            new RemoteIdentityReconciler(analyzer),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(
                new StubLocalReader([1, 2, 3, 4, 5], fileLength: 5),
                new StubRemoteReader([1, 2, 3, 4, 5])));

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await coordinator.CoordinateAsync(PreparedTask(), cancellation.Token));

        Assert.AreEqual(0, analyzer.AnalysisCount);
    }

    private static DownloadTask PreparedTask(long confirmedBytes = 5, long remoteLength = 5) =>
        DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Downloading,
            confirmedBytes,
            "C:\\Downloads\\file.download",
            new RemoteIdentity(
                new Uri("https://cdn.example.test/file.bin"),
                remoteLength,
                "\"v1\"",
                null,
                supportsByteRanges: true));

    private static void AssertTaskUnchanged(DownloadTask task)
    {
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
    }

    private sealed class Fixture
    {
        public Fixture(
            TemporaryFileSnapshot snapshot,
            byte[] localContent,
            long observedLength = 5,
            byte[]? remoteContent = null,
            long? localRangeFileLength = null)
        {
            Inspector = new StubInspector(snapshot);
            Analyzer = new StubAnalyzer(observedLength);
            LocalReader = new StubLocalReader(
                localContent,
                localRangeFileLength ?? snapshot.Length ?? 0);
            RemoteReader = new StubRemoteReader(remoteContent ?? localContent);
            Coordinator = new StartupRecoveryCoordinator(
                new StartupRecoveryReconciler(Inspector),
                new RemoteIdentityReconciler(Analyzer),
                new RecoveryDecisionEvaluator(),
                new RecoveryOverlapVerifier(LocalReader, RemoteReader));
        }

        public StubInspector Inspector { get; }
        public StubAnalyzer Analyzer { get; }
        public StubLocalReader LocalReader { get; }
        public StubRemoteReader RemoteReader { get; }
        public StartupRecoveryCoordinator Coordinator { get; }
    }

    private sealed class StubInspector(TemporaryFileSnapshot snapshot) : ITemporaryFileInspector
    {
        public int InspectionCount { get; private set; }

        public ValueTask<TemporaryFileSnapshot> InspectAsync(
            string temporaryPath,
            CancellationToken cancellationToken)
        {
            InspectionCount++;
            return ValueTask.FromResult(snapshot);
        }
    }

    private sealed class CancellingInspector(CancellationTokenSource cancellation) : ITemporaryFileInspector
    {
        public ValueTask<TemporaryFileSnapshot> InspectAsync(
            string temporaryPath,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            return ValueTask.FromResult(TemporaryFileSnapshot.Existing(5));
        }
    }

    private sealed class StubAnalyzer(long observedLength) : IRemoteResourceAnalyzer
    {
        public int AnalysisCount { get; private set; }

        public ValueTask<RemoteResourceInfo> AnalyzeAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            AnalysisCount++;
            return ValueTask.FromResult(
                new RemoteResourceInfo(
                    uri,
                    new Uri("https://cdn.example.test/file.bin"),
                    observedLength,
                    null,
                    null,
                    "\"v1\"",
                    null,
                    SupportsByteRanges: true));
        }
    }

    private sealed class StubLocalReader(byte[] content, long fileLength) : ITemporaryFileRangeReader
    {
        public int ReadCount { get; private set; }

        public ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
            string temporaryPath,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult(new TemporaryFileRangeSnapshot(fileLength, content));
        }
    }

    private sealed class StubRemoteReader(byte[] content) : IRemoteRangeReader
    {
        public int ReadCount { get; private set; }

        public ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
            RemoteIdentity identity,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult<ReadOnlyMemory<byte>>(content);
        }
    }
}
