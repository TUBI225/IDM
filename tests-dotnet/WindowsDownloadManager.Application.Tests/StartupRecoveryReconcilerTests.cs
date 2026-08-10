using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class StartupRecoveryReconcilerTests
{
    [TestMethod]
    public async Task Reconcile_RecoveryMetadataAbsent_DoesNotInspectDisk()
    {
        var inspector = new StubInspector(TemporaryFileSnapshot.Existing(10));
        var reconciler = new StartupRecoveryReconciler(inspector);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Downloading,
            confirmedBytes: 5);

        var result = await reconciler.ReconcileAsync(task, CancellationToken.None);

        Assert.AreEqual(TemporaryFileReconciliationStatus.RecoveryMetadataAbsent, result.Status);
        Assert.AreEqual(0, result.SafePosition);
        Assert.IsNull(result.FileLength);
        Assert.AreEqual(0, inspector.InspectionCount);
        AssertTaskUnchanged(task);
    }

    [TestMethod]
    public Task Reconcile_TemporaryFileAbsent_ClassifiesWithoutMutation() =>
        AssertClassificationAsync(
            TemporaryFileSnapshot.Absent,
            TemporaryFileReconciliationStatus.TemporaryFileAbsent,
            expectedFileLength: null,
            expectedSafePosition: 0);

    [TestMethod]
    public Task Reconcile_TemporaryFileShorter_UsesFileLengthAsSafePosition() =>
        AssertClassificationAsync(
            TemporaryFileSnapshot.Existing(4),
            TemporaryFileReconciliationStatus.TemporaryFileShorter,
            expectedFileLength: 4,
            expectedSafePosition: 4);

    [TestMethod]
    public Task Reconcile_TemporaryFileMatchesCheckpoint_UsesConfirmedPosition() =>
        AssertClassificationAsync(
            TemporaryFileSnapshot.Existing(5),
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            expectedFileLength: 5,
            expectedSafePosition: 5);

    [TestMethod]
    public Task Reconcile_TemporaryFileLonger_KeepsCheckpointAsSafePosition() =>
        AssertClassificationAsync(
            TemporaryFileSnapshot.Existing(6),
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            expectedFileLength: 6,
            expectedSafePosition: 5);

    private static async Task AssertClassificationAsync(
        TemporaryFileSnapshot snapshot,
        TemporaryFileReconciliationStatus expectedStatus,
        long? expectedFileLength,
        long expectedSafePosition)
    {
        var inspector = new StubInspector(snapshot);
        var reconciler = new StartupRecoveryReconciler(inspector);
        var task = PreparedTask();

        var result = await reconciler.ReconcileAsync(task, CancellationToken.None);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedFileLength, result.FileLength);
        Assert.AreEqual(expectedSafePosition, result.SafePosition);
        Assert.AreEqual(task.Id, result.DownloadId);
        Assert.AreEqual(task.TemporaryPath, result.TemporaryPath);
        Assert.AreEqual(5, result.ConfirmedBytes);
        Assert.AreEqual(1, inspector.InspectionCount);
        AssertTaskUnchanged(task);
    }

    private static DownloadTask PreparedTask() => DownloadTask.Restore(
        Guid.NewGuid(),
        new Uri("https://example.test/file.bin"),
        "C:\\Downloads\\file.bin",
        DownloadState.Downloading,
        confirmedBytes: 5,
        "C:\\Downloads\\file.download",
        new RemoteIdentity(
            new Uri("https://cdn.example.test/file.bin"),
            10,
            "\"v1\"",
            null,
            supportsByteRanges: true));

    private static void AssertTaskUnchanged(DownloadTask task)
    {
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
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
}
