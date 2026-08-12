using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadFinalizationCoordinatorTests
{
    private const string VerifiedSha256 = "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824";

    [TestMethod]
    public async Task Finalize_ValidFile_PersistsIntentBeforeMoveAndCompletionAfterMove()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.FinalizeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
        Assert.AreEqual(VerifiedSha256, task.VerifiedSha256);
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
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await coordinator.FinalizeAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task Finalize_KeepBoth_SelectsFirstAvailableNameBeforePersistingIntent()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new CollisionInspector(),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.FinalizeAsync(
            task,
            expectedSha256: null,
            DestinationCollisionPolicy.KeepBoth,
            CancellationToken.None);

        Assert.AreEqual("C:\\Downloads\\file (2).bin", task.DestinationPath);
        Assert.AreEqual(DownloadState.Completed, task.State);
        CollectionAssert.AreEqual(
            new[] { "save:Finalizing", "move", "save:Completed" },
            events);
    }

    [TestMethod]
    public async Task Repair_DifferentVolumeBothVerified_DelegatesCleanupAndCompletes()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Finalizing);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: true),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.RepairAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
        CollectionAssert.AreEqual(new[] { "repair", "save:Completed" }, events);
    }

    [TestMethod]
    public async Task Repair_DestinationOnly_CompletesPersistedIntentWithoutMovingAgain()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Finalizing);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: false, destinationExists: true),
            new StubHasher(VerifiedSha256),
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
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events, rejectRepair: true),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.RepairAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Finalizing, task.State);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task Finalize_ExpectedSha256Mismatch_DoesNotPersistIntentOrMove()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.FinalizeAsync(
                task,
                new string('0', 64),
                DestinationCollisionPolicy.Fail,
                CancellationToken.None));

        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.IsNull(task.VerifiedSha256);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task Repair_PersistedSha256Mismatch_RemainsFinalizingWithoutMove()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Finalizing);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(new string('0', 64)),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.RepairAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Finalizing, task.State);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task Finalize_DestinationHashChangesAfterMove_RemainsFinalizing()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new SequenceHasher(VerifiedSha256, new string('0', 64)),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.FinalizeAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Finalizing, task.State);
        CollectionAssert.AreEqual(new[] { "save:Finalizing", "move" }, events);
    }

    private static DownloadTask TaskIn(DownloadState state, string? remoteSha256 = null) =>
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
                supportsByteRanges: true,
                sha256: remoteSha256),
            state is DownloadState.Finalizing or DownloadState.Completed
                ? VerifiedSha256
                : null);

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

    private sealed class CollisionInspector : ITemporaryFileInspector
    {
        public ValueTask<TemporaryFileSnapshot> InspectAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var exists = path.EndsWith(".download", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("file.bin", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("file (1).bin", StringComparison.OrdinalIgnoreCase);
            return ValueTask.FromResult(exists
                ? TemporaryFileSnapshot.Existing(5)
                : TemporaryFileSnapshot.Absent);
        }
    }

    [TestMethod]
    public async Task Finalize_HashMismatch_Throws()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        var mismatchingExpectedHash = "1111111111111111111111111111111111111111111111111111111111111111";

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.FinalizeAsync(
                task,
                mismatchingExpectedHash,
                DestinationCollisionPolicy.Fail,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task Finalize_RemoteIdentitySha256Matches_ByDefaultSucceeds()
    {
        var events = new List<string>();
        var task = TaskIn(DownloadState.Verifying, remoteSha256: VerifiedSha256);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await coordinator.FinalizeAsync(task, CancellationToken.None);

        Assert.AreEqual(DownloadState.Completed, task.State);
        CollectionAssert.AreEqual(
            new[] { "save:Finalizing", "move", "save:Completed" },
            events);
    }

    [TestMethod]
    public async Task Finalize_RemoteIdentitySha256Mismatch_ByDefaultThrows()
    {
        var events = new List<string>();
        const string mismatchingRemoteSha256 = "1111111111111111111111111111111111111111111111111111111111111111";
        var task = TaskIn(DownloadState.Verifying, remoteSha256: mismatchingRemoteSha256);
        var coordinator = new DownloadFinalizationCoordinator(
            new StubInspector(temporaryExists: true, destinationExists: false),
            new StubHasher(VerifiedSha256),
            new RecordingFinalizer(events),
            new RecordingRepository(events));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await coordinator.FinalizeAsync(task, CancellationToken.None));

        Assert.AreEqual(DownloadState.Verifying, task.State);
        Assert.IsNull(task.VerifiedSha256);
        Assert.AreEqual(0, events.Count);
    }

    private sealed class RecordingFinalizer(List<string> events, bool rejectRepair = false) : ITemporaryFileFinalizer
    {
        public ValueTask FinalizeAsync(
            Guid downloadId,
            string temporaryPath,
            string destinationPath,
            string verifiedSha256,
            CancellationToken cancellationToken)
        {
            events.Add("move");
            return ValueTask.CompletedTask;
        }

        public ValueTask RepairAsync(
            Guid downloadId,
            string temporaryPath,
            string destinationPath,
            string verifiedSha256,
            CancellationToken cancellationToken)
        {
            if (rejectRepair)
            {
                throw new InvalidDataException("Ambiguous same-volume state.");
            }

            events.Add("repair");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StubHasher(string hash) : ITemporaryFileHasher
    {
        public ValueTask<string> ComputeSha256Async(string path, CancellationToken cancellationToken) =>
            ValueTask.FromResult(hash);
    }

    private sealed class SequenceHasher(params string[] hashes) : ITemporaryFileHasher
    {
        private int _index;

        public ValueTask<string> ComputeSha256Async(string path, CancellationToken cancellationToken) =>
            ValueTask.FromResult(hashes[_index++]);
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
