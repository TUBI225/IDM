using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadFinalizationCoordinatorTests
{
    [TestMethod]
    public async Task Finalize_ValidFile_PersistsIntentBeforeMoveAndCompletionAfterMove()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.FinalizeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
        CollectionAssert.AreEqual(
            new[] { "save:Finalizing", "move", "save:Completed" },
            events);
    }

    [TestMethod]
    public async Task Finalize_DestinationExists_DoesNotPersistIntentOrMove()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: true),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await coordinator.FinalizeAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task Repair_DestinationOnly_CompletesPersistedIntentWithoutMovingAgain()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Finalizing);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: false, destinationExists: true),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.RepairAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
        CollectionAssert.AreEqual(new[] { "save:Completed" }, events);
    }

    [TestMethod]
    public async Task Repair_AmbiguousFiles_RemainsFinalizing()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Finalizing);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: true),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.RepairAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Finalizing, task.State);
        Assert.AreEqual(0, events.Count);
    }

    private static DownloadTask TaskIn(DownloadState state) =>
        DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            state,
            confirmedBytes: 5,
            "C:\\Downloads\\file.download",
            new RemoteIdentity(
                new Uri("https://example.test/file.bin"),
                5,
                "\"v1\"",
                null,
                supportsByteRanges: true));

    private sealed class StubInspector(bool temporaryExists, bool destinationExists) : ITemporaryFileInspector
    {
        public ValueTask<TemporaryFileSnapshot> InspectAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var exists = path.EndsWith(".download", StringComparison.OrdinalIgnoreCase)
                ? temporaryExists
                : destinationExists;
            return ValueTask.FromResult(exists
                ? TemporaryFileSnapshot.Existing(5)
                : TemporaryFileSnapshot.Absent);
        }
    }

    private sealed class RecordingFinalizer(List<string> events) : ITemporaryFileFinalizer
    {
        public ValueTask MoveAtomicallyAsync(
            string temporaryPath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            events.Add("move");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingRepository(List<string> events) : IDownloadRepository
    {
        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            ValueTask.FromResult<DownloadTask?>(null);

        public ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            events.Add($"save:{task.State}");
            return ValueTask.CompletedTask;
        }
    }
}
