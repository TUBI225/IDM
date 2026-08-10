using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Persistence.Sqlite;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Integration.Tests;

[TestClass]
public sealed class DurabilityFaultInjectionIntegrationTests
{
    private static readonly byte[] Content = "hello"u8.ToArray();
    private static readonly byte[] LargeContent = CreateLargeContent();
    private static readonly byte[] FirstLargeBlockContent = LargeContent[..65_536];

    [TestMethod]
    public async Task RunNew_FaultAfterDurableFlush_RestoresCheckpointBehindFile()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "after-flush.download");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        var task = NewTask(directory.Path);
        await using (var repository = new SqliteDownloadRepository(databasePath))
        {
            var writer = new FaultAfterFlushWriter(new DurableTemporaryFileWriter());
            var orchestrator = CreateOrchestrator(writer, repository);

            await Assert.ThrowsExactlyAsync<InjectedDurabilityFaultException>(async () =>
                await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None));
        }

        var restored = await RestoreAsync(databasePath, task.Id);
        var reconciliation = await ReconcileAsync(restored);

        Assert.AreEqual(0, task.ConfirmedBytes);
        Assert.AreEqual(0, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Downloading, restored.State);
        Assert.AreEqual(Content.Length, new FileInfo(temporaryPath).Length);
        CollectionAssert.AreEqual(Content, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(TemporaryFileReconciliationStatus.TemporaryFileLonger, reconciliation.Status);
        Assert.AreEqual(0, reconciliation.SafePosition);
    }

    [TestMethod]
    public async Task RunNew_FaultAfterSecondBlockDurableFlush_RestoresFirstCheckpoint()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "after-second-block-flush.download");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        var task = NewTask(directory.Path);
        await using (var repository = new SqliteDownloadRepository(databasePath))
        {
            var writer = new FaultAfterFlushWriter(new DurableTemporaryFileWriter(), targetWriteCount: 2);
            var orchestrator = CreateOrchestrator(writer, repository, LargeContent);

            await Assert.ThrowsExactlyAsync<InjectedDurabilityFaultException>(async () =>
                await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None));
        }

        var restored = await RestoreAsync(databasePath, task.Id);
        var reconciliation = await ReconcileAsync(restored);

        Assert.AreEqual(65_536, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Downloading, restored.State);
        Assert.AreEqual(LargeContent.Length, new FileInfo(temporaryPath).Length);
        CollectionAssert.AreEqual(LargeContent, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(TemporaryFileReconciliationStatus.TemporaryFileLonger, reconciliation.Status);
        Assert.AreEqual(65_536, reconciliation.SafePosition);
    }

    [TestMethod]
    public async Task RunNew_FaultBeforeCheckpointCommit_RestoresCheckpointBehindFile()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "before-checkpoint.download");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        var task = NewTask(directory.Path);
        await using (var innerRepository = new SqliteDownloadRepository(databasePath))
        {
            var repository = new FaultInjectingRepository(
                innerRepository,
                CheckpointFaultBoundary.BeforeCommit);
            var orchestrator = CreateOrchestrator(new DurableTemporaryFileWriter(), repository);

            await Assert.ThrowsExactlyAsync<InjectedDurabilityFaultException>(async () =>
                await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None));
        }

        var restored = await RestoreAsync(databasePath, task.Id);
        var reconciliation = await ReconcileAsync(restored);

        Assert.AreEqual(Content.Length, task.ConfirmedBytes);
        Assert.AreEqual(0, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Downloading, restored.State);
        Assert.AreEqual(Content.Length, new FileInfo(temporaryPath).Length);
        CollectionAssert.AreEqual(Content, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(TemporaryFileReconciliationStatus.TemporaryFileLonger, reconciliation.Status);
        Assert.AreEqual(0, reconciliation.SafePosition);
    }

    [TestMethod]
    public async Task RunNew_FaultAfterCheckpointCommit_RestoresExactDurableCheckpoint()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "after-checkpoint.download");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        var task = NewTask(directory.Path);
        await using (var innerRepository = new SqliteDownloadRepository(databasePath))
        {
            var repository = new FaultInjectingRepository(
                innerRepository,
                CheckpointFaultBoundary.AfterCommit);
            var orchestrator = CreateOrchestrator(new DurableTemporaryFileWriter(), repository);

            await Assert.ThrowsExactlyAsync<InjectedDurabilityFaultException>(async () =>
                await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None));
        }

        var restored = await RestoreAsync(databasePath, task.Id);
        var reconciliation = await ReconcileAsync(restored);

        Assert.AreEqual(Content.Length, task.ConfirmedBytes);
        Assert.AreEqual(Content.Length, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Downloading, restored.State);
        Assert.AreEqual(Content.Length, new FileInfo(temporaryPath).Length);
        CollectionAssert.AreEqual(Content, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            reconciliation.Status);
        Assert.AreEqual(Content.Length, reconciliation.SafePosition);
    }

    [TestMethod]
    public Task RunNew_ProcessKilledAfterDurableFlush_RestoresCheckpointBehindFile() =>
        AssertAbruptTerminationAsync(
            "AfterDurableFlush",
            Content,
            expectedCheckpoint: 0,
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            expectedSafePosition: 0);

    [TestMethod]
    public Task RunNew_ProcessKilledBeforeCheckpointCommit_RestoresCheckpointBehindFile() =>
        AssertAbruptTerminationAsync(
            "BeforeCheckpointCommit",
            Content,
            expectedCheckpoint: 0,
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            expectedSafePosition: 0);

    [TestMethod]
    public Task RunNew_ProcessKilledAfterCheckpointCommit_RestoresExactDurableCheckpoint() =>
        AssertAbruptTerminationAsync(
            "AfterCheckpointCommit",
            Content,
            expectedCheckpoint: Content.Length,
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            expectedSafePosition: Content.Length);

    [TestMethod]
    public Task RunNew_ProcessKilledAfterSecondBlockFlush_RestoresFirstCheckpoint() =>
        AssertAbruptTerminationAsync(
            "AfterSecondBlockDurableFlush",
            LargeContent,
            expectedCheckpoint: 65_536,
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            expectedSafePosition: 65_536);

    [TestMethod]
    public Task RunNew_ProcessKilledBeforeSecondBlockWriteAndFlush_RestoresFirstBlockExactly() =>
        AssertAbruptTerminationAsync(
            "BeforeSecondBlockWriteAndFlush",
            FirstLargeBlockContent,
            expectedCheckpoint: 65_536,
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            expectedSafePosition: 65_536);

    [TestMethod]
    public Task RunNew_ProcessKilledBeforeSecondCheckpointCommit_RestoresFirstCheckpoint() =>
        AssertAbruptTerminationAsync(
            "BeforeSecondCheckpointCommit",
            LargeContent,
            expectedCheckpoint: 65_536,
            TemporaryFileReconciliationStatus.TemporaryFileLonger,
            expectedSafePosition: 65_536);

    [TestMethod]
    public Task RunNew_ProcessKilledAfterSecondCheckpointCommit_RestoresSecondCheckpoint() =>
        AssertAbruptTerminationAsync(
            "AfterSecondCheckpointCommit",
            LargeContent,
            expectedCheckpoint: 70_000,
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            expectedSafePosition: 70_000);

    private static async Task AssertAbruptTerminationAsync(
        string boundary,
        byte[] expectedContent,
        long expectedCheckpoint,
        TemporaryFileReconciliationStatus expectedStatus,
        long expectedSafePosition)
    {
        using var directory = new TemporaryDirectory();
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        var temporaryPath = Path.Combine(directory.Path, $"{boundary}.download");
        var taskId = Guid.NewGuid();
        var projectRoot = FindProjectRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Release";
        var hostAssembly = Path.Combine(
            projectRoot,
            "tests-dotnet",
            "WindowsDownloadManager.CrashTestHost",
            "bin",
            configuration,
            "net10.0",
            "WindowsDownloadManager.CrashTestHost.dll");
        Assert.IsTrue(File.Exists(hostAssembly), $"Crash test host not found: {hostAssembly}");
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(projectRoot, ".tools", "dotnet", "dotnet.exe"),
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(hostAssembly);
        startInfo.ArgumentList.Add(boundary);
        startInfo.ArgumentList.Add(taskId.ToString("D"));
        startInfo.ArgumentList.Add(databasePath);
        startInfo.ArgumentList.Add(temporaryPath);
        using var process = Process.Start(startInfo) ??
            throw new AssertFailedException("The crash test host could not be started.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new AssertFailedException("The crash test host did not terminate within 30 seconds.");
        }

        var standardError = await process.StandardError.ReadToEndAsync();
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        Assert.AreNotEqual(
            0,
            process.ExitCode,
            $"The crash host exited normally. stdout={standardOutput}; stderr={standardError}");
        var restored = await RestoreAsync(databasePath, taskId);
        var reconciliation = await ReconcileAsync(restored);

        Assert.AreEqual(expectedCheckpoint, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Downloading, restored.State);
        Assert.AreEqual(expectedContent.Length, new FileInfo(temporaryPath).Length);
        CollectionAssert.AreEqual(expectedContent, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(expectedStatus, reconciliation.Status);
        Assert.AreEqual(expectedSafePosition, reconciliation.SafePosition);
    }

    private static string FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WindowsDownloadManager.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new AssertFailedException("The project root could not be located.");
    }

    private static byte[] CreateLargeContent()
    {
        var content = new byte[70_000];
        for (var index = 0; index < content.Length; index++)
        {
            content[index] = (byte)(index % 251);
        }

        return content;
    }

    private static DownloadOrchestrator CreateOrchestrator(
        ITemporaryFileWriter writer,
        IDownloadRepository repository,
        byte[]? content = null)
    {
        var targetContent = content ?? Content;
        return new(
            new StubAnalyzer(targetContent),
            new StubContentSource(targetContent),
            writer,
            repository);
    }

    private static DownloadTask NewTask(string directoryPath) =>
        new(
            Guid.NewGuid(),
            new Uri("https://example.test/fixture.bin"),
            Path.Combine(directoryPath, "fixture.bin"));

    private static async Task<DownloadTask> RestoreAsync(string databasePath, Guid taskId)
    {
        await using var repository = new SqliteDownloadRepository(databasePath);
        return await repository.FindAsync(taskId, CancellationToken.None) ??
            throw new AssertFailedException("The interrupted download was not restored from SQLite.");
    }

    private static async Task<TemporaryFileReconciliationResult> ReconcileAsync(DownloadTask task) =>
        await new StartupRecoveryReconciler(new ReadOnlyTemporaryFileInspector())
            .ReconcileAsync(task, CancellationToken.None);

    private sealed class StubAnalyzer(byte[] content) : IRemoteResourceAnalyzer
    {
        public ValueTask<RemoteResourceInfo> AnalyzeAsync(
            Uri uri,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                new RemoteResourceInfo(
                    uri,
                    uri,
                    content.Length,
                    null,
                    null,
                    "\"v1\"",
                    null,
                    SupportsByteRanges: true));
    }

    private sealed class StubContentSource(byte[] content) : IRemoteContentSource
    {
        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(content, writable: false), content.Length));
    }

    private sealed class FaultAfterFlushWriter(
        ITemporaryFileWriter inner,
        int targetWriteCount = 1) : ITemporaryFileWriter
    {
        private int _writeCount;

        public ValueTask PrepareNewAsync(
            string temporaryPath,
            CancellationToken cancellationToken) =>
            inner.PrepareNewAsync(temporaryPath, cancellationToken);

        public async ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            _writeCount++;
            var boundary = await inner.WriteAndFlushAsync(temporaryPath, offset, content, cancellationToken);
            if (_writeCount == targetWriteCount)
            {
                throw new InjectedDurabilityFaultException("Fault injected after the durable flush.");
            }

            return boundary;
        }
    }

    private sealed class FaultInjectingRepository(
        IDownloadRepository inner,
        CheckpointFaultBoundary boundary) : IDownloadRepository
    {
        private bool _faultInjected;

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(id, cancellationToken);

        public async ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            var isFirstPositiveCheckpoint = !_faultInjected &&
                task.State == DownloadState.Downloading &&
                task.ConfirmedBytes > 0;
            if (!isFirstPositiveCheckpoint)
            {
                await inner.SaveAsync(task, cancellationToken);
                return;
            }

            _faultInjected = true;
            if (boundary == CheckpointFaultBoundary.BeforeCommit)
            {
                throw new InjectedDurabilityFaultException(
                    "Fault injected before the SQLite checkpoint commit.");
            }

            await inner.SaveAsync(task, cancellationToken);
            throw new InjectedDurabilityFaultException(
                "Fault injected after the SQLite checkpoint commit.");
        }
    }

    private enum CheckpointFaultBoundary
    {
        BeforeCommit,
        AfterCommit,
    }

    private sealed class InjectedDurabilityFaultException(string message) : IOException(message);
}
