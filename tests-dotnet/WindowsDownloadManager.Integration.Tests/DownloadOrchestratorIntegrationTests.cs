using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Network.Http;
using WindowsDownloadManager.Persistence.Sqlite;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Integration.Tests;

[TestClass]
public sealed class DownloadOrchestratorIntegrationTests
{
    [TestMethod]
    public async Task Finalize_KeepBothAcrossVolumes_PersistsResolvedDestinationAndExactFile()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var requestedDestination = Path.Combine(directory.Path, "fixture.bin");
        var resolvedDestination = Path.Combine(directory.Path, "fixture (1).bin");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        await File.WriteAllBytesAsync(temporaryPath, "hello"u8.ToArray());
        await File.WriteAllBytesAsync(requestedDestination, "existing"u8.ToArray());
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/fixture.bin"),
            requestedDestination,
            DownloadState.Verifying,
            confirmedBytes: 5,
            temporaryPath,
            new RemoteIdentity(
                new Uri("https://example.test/fixture.bin"),
                5,
                "\"v1\"",
                null,
                supportsByteRanges: true));
        await using var repository = new SqliteDownloadRepository(databasePath);
        var finalization = new DownloadFinalizationCoordinator(
            new ReadOnlyTemporaryFileInspector(),
            new Sha256TemporaryFileHasher(),
            new AtomicTemporaryFileFinalizer(new DifferentVolumeComparer()),
            repository);

        await finalization.FinalizeAsync(
            task,
            expectedSha256: null,
            DestinationCollisionPolicy.KeepBoth,
            CancellationToken.None);
        var restored = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsFalse(File.Exists(temporaryPath));
        CollectionAssert.AreEqual("existing"u8.ToArray(), await File.ReadAllBytesAsync(requestedDestination));
        CollectionAssert.AreEqual("hello"u8.ToArray(), await File.ReadAllBytesAsync(resolvedDestination));
        Assert.IsNotNull(restored);
        Assert.AreEqual(DownloadState.Completed, restored.State);
        Assert.AreEqual(resolvedDestination, restored.DestinationPath);
        Assert.AreEqual(
            "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824",
            restored.VerifiedSha256);
    }

    [TestMethod]
    public async Task ResumeAndFinalize_RestoredTask_AppendsThenAtomicallyCompletes()
    {
        await using var server = new SequentialLoopbackServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-0/5\r\nContent-Length: 1\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nh",
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-2/5\r\nContent-Length: 3\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nhel",
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 3-4/5\r\nContent-Length: 2\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nlo");
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var destinationPath = Path.Combine(directory.Path, "fixture.bin");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        await File.WriteAllBytesAsync(temporaryPath, "hel"u8.ToArray());
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler, disposeHandler: false);
        var safetyValidator = new AllowAllUriSafetyValidator();
        var analyzer = new HttpRemoteResourceAnalyzer(client, safetyValidator);
        var remoteSource = new HttpRemoteContentSource(client, safetyValidator);
        await using var repository = new SqliteDownloadRepository(databasePath);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            server.Uri,
            destinationPath,
            DownloadState.Downloading,
            confirmedBytes: 3,
            temporaryPath,
            new RemoteIdentity(server.Uri, 5, "\"v1\"", null, supportsByteRanges: true));
        await repository.SaveAsync(task, CancellationToken.None);
        var recovery = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(new ReadOnlyTemporaryFileInspector()),
            new RemoteIdentityReconciler(analyzer),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(new ReadOnlyTemporaryFileRangeReader(), remoteSource));
        var orchestrator = new DownloadOrchestrator(
            analyzer,
            remoteSource,
            new DurableTemporaryFileWriter(),
            repository,
            recovery);

        var resume = await orchestrator.ResumeAsync(task, CancellationToken.None);
        var afterResume = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.AreEqual(DownloadResumeStatus.ResumedToVerification, resume.Status);
        Assert.IsNotNull(afterResume);
        Assert.AreEqual(DownloadState.Verifying, afterResume.State);
        Assert.AreEqual(5, afterResume.ConfirmedBytes);
        CollectionAssert.AreEqual("hello"u8.ToArray(), await File.ReadAllBytesAsync(temporaryPath));
        Assert.IsTrue(server.Requests[2].Contains("Range: bytes=3-", StringComparison.OrdinalIgnoreCase));

        var finalization = new DownloadFinalizationCoordinator(
            new ReadOnlyTemporaryFileInspector(),
            new Sha256TemporaryFileHasher(),
            new AtomicTemporaryFileFinalizer(),
            repository);
        await finalization.FinalizeAsync(task, CancellationToken.None);
        var completed = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsFalse(File.Exists(temporaryPath));
        CollectionAssert.AreEqual("hello"u8.ToArray(), await File.ReadAllBytesAsync(destinationPath));
        Assert.IsNotNull(completed);
        Assert.AreEqual(DownloadState.Completed, completed.State);
        Assert.AreEqual(5, completed.ConfirmedBytes);
        Assert.AreEqual(
            "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824",
            completed.VerifiedSha256);
    }

    [TestMethod]
    public async Task CoordinateRecovery_RestoredTask_UsesBoundedRangeAndDoesNotMutateState()
    {
        await using var server = new SequentialLoopbackServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-0/5\r\nContent-Length: 1\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nh",
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-4/5\r\nContent-Length: 5\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nhello");
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var content = "hello"u8.ToArray();
        await File.WriteAllBytesAsync(temporaryPath, content);
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler, disposeHandler: false);
        var safetyValidator = new AllowAllUriSafetyValidator();
        var remoteSource = new HttpRemoteContentSource(client, safetyValidator);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            server.Uri,
            Path.Combine(directory.Path, "fixture.bin"),
            DownloadState.Downloading,
            confirmedBytes: 5,
            temporaryPath,
            new RemoteIdentity(server.Uri, 5, "\"v1\"", null, supportsByteRanges: true));
        var coordinator = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(new ReadOnlyTemporaryFileInspector()),
            new RemoteIdentityReconciler(new HttpRemoteResourceAnalyzer(client, safetyValidator)),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(new ReadOnlyTemporaryFileRangeReader(), remoteSource));

        var result = await coordinator.CoordinateAsync(task, CancellationToken.None);

        Assert.AreEqual(StartupRecoveryAssessmentStatus.OverlapMatched, result.Status);
        Assert.IsNotNull(result.Overlap);
        Assert.AreEqual(OverlapVerificationStatus.Match, result.Overlap.Status);
        Assert.AreEqual(0, result.Overlap.Offset);
        Assert.AreEqual(5, result.Overlap.Length);
        Assert.AreEqual(2, server.Requests.Count);
        Assert.IsTrue(server.Requests[0].Contains("Range: bytes=0-0", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(server.Requests[1].Contains("Range: bytes=0-4", StringComparison.OrdinalIgnoreCase));
        CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
    }

    [TestMethod]
    public async Task ReconcileRemote_UsesSingleProbeAndDoesNotTouchTemporaryFile()
    {
        await using var server = new SequentialLoopbackServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-0/5\r\nContent-Length: 1\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nh");
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var content = "hello"u8.ToArray();
        await File.WriteAllBytesAsync(temporaryPath, content);
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler, disposeHandler: false);
        var analyzer = new HttpRemoteResourceAnalyzer(client, new AllowAllUriSafetyValidator());
        var reconciler = new RemoteIdentityReconciler(analyzer);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            server.Uri,
            Path.Combine(directory.Path, "fixture.bin"),
            DownloadState.Downloading,
            confirmedBytes: 5,
            temporaryPath,
            new RemoteIdentity(server.Uri, 5, "\"v1\"", null, supportsByteRanges: true));

        var result = await reconciler.ReconcileAsync(task, CancellationToken.None);

        Assert.AreEqual(RemoteIdentityReconciliationStatus.Compatible, result.Status);
        Assert.AreEqual(RemoteIdentityDifference.None, result.Differences);
        Assert.AreEqual(1, server.Requests.Count);
        Assert.IsTrue(server.Requests[0].Contains("Range: bytes=0-0", StringComparison.OrdinalIgnoreCase));
        CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
    }

    [TestMethod]
    public async Task Reconcile_RestoredTaskWithLongerFile_ReturnsCheckpointWithoutWriting()
    {
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var destinationPath = Path.Combine(directory.Path, "fixture.bin");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        await File.WriteAllBytesAsync(temporaryPath, "1234567"u8.ToArray());
        await using var repository = new SqliteDownloadRepository(databasePath);
        var task = new DownloadTask(
            Guid.NewGuid(),
            new Uri("https://example.test/fixture.bin"),
            destinationPath);
        task.TransitionTo(DownloadState.Analyzing);
        task.TransitionTo(DownloadState.Preparing);
        task.RecordPreparation(
            temporaryPath,
            new RemoteIdentity(
                new Uri("https://cdn.example.test/fixture.bin"),
                10,
                "\"v1\"",
                null,
                supportsByteRanges: true));
        task.TransitionTo(DownloadState.Waiting);
        task.TransitionTo(DownloadState.Downloading);
        task.ConfirmPersistedBytes(5);
        await repository.SaveAsync(task, CancellationToken.None);
        var restored = await repository.FindAsync(task.Id, CancellationToken.None);
        Assert.IsNotNull(restored);
        var reconciler = new StartupRecoveryReconciler(new ReadOnlyTemporaryFileInspector());

        var result = await reconciler.ReconcileAsync(restored, CancellationToken.None);
        var storedAfterReconciliation = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.AreEqual(TemporaryFileReconciliationStatus.TemporaryFileLonger, result.Status);
        Assert.AreEqual(7, result.FileLength);
        Assert.AreEqual(5, result.SafePosition);
        CollectionAssert.AreEqual("1234567"u8.ToArray(), await File.ReadAllBytesAsync(temporaryPath));
        Assert.IsNotNull(storedAfterReconciliation);
        Assert.AreEqual(DownloadState.Downloading, storedAfterReconciliation.State);
        Assert.AreEqual(5, storedAfterReconciliation.ConfirmedBytes);
    }

    [TestMethod]
    public async Task RunNew_NetworkToDurableFileToSqlite_PreservesExactCheckpoint()
    {
        await using var server = new SequentialLoopbackServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-0/5\r\nContent-Length: 1\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nh",
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-4/5\r\nContent-Length: 5\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nhello");
        using var directory = new TemporaryDirectory();
        var temporaryPath = Path.Combine(directory.Path, "fixture.download");
        var destinationPath = Path.Combine(directory.Path, "fixture.bin");
        var databasePath = Path.Combine(directory.Path, "downloads.sqlite3");
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler, disposeHandler: false);
        var safetyValidator = new AllowAllUriSafetyValidator();
        await using var repository = new SqliteDownloadRepository(databasePath);
        var orchestrator = new DownloadOrchestrator(
            new HttpRemoteResourceAnalyzer(client, safetyValidator),
            new HttpRemoteContentSource(client, safetyValidator),
            new DurableTemporaryFileWriter(),
            repository);
        var task = new DownloadTask(Guid.NewGuid(), server.Uri, destinationPath);

        var result = await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None);
        var restored = await repository.FindAsync(task.Id, CancellationToken.None);

        CollectionAssert.AreEqual("hello"u8.ToArray(), await File.ReadAllBytesAsync(temporaryPath));
        Assert.AreEqual(5, result.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Verifying, result.State);
        Assert.IsNotNull(restored);
        Assert.AreEqual(5, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Verifying, restored.State);
        Assert.AreEqual(temporaryPath, restored.TemporaryPath);
        Assert.IsNotNull(restored.RemoteIdentity);
        Assert.AreEqual(server.Uri, restored.RemoteIdentity.FinalUri);
        Assert.AreEqual(5, restored.RemoteIdentity.Length);
        Assert.AreEqual("\"v1\"", restored.RemoteIdentity.EntityTag);
        Assert.IsTrue(restored.RemoteIdentity.SupportsByteRanges);
        Assert.AreEqual(2, server.Requests.Count);
        Assert.IsTrue(server.Requests[0].Contains("Range: bytes=0-0", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(server.Requests[1].Contains("Range: bytes=0-", StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class DifferentVolumeComparer : IFileVolumeComparer
{
    public bool AreOnSameVolume(string firstPath, string secondPath) => false;
}

internal sealed class AllowAllUriSafetyValidator : IUriSafetyValidator
{
    public ValueTask ValidateAsync(Uri uri, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

internal sealed class SequentialLoopbackServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _serverTask;

    public SequentialLoopbackServer(params string[] responses)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Uri = new Uri($"http://127.0.0.1:{port}/file.bin");
        _serverTask = ServeAsync(responses);
    }

    public Uri Uri { get; }
    public List<string> Requests { get; } = [];

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        await _serverTask.ConfigureAwait(false);
    }

    private async Task ServeAsync(string[] responses)
    {
        try
        {
            foreach (var response in responses)
            {
                using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                await using var stream = client.GetStream();
                using var reader = new StreamReader(
                    stream,
                    Encoding.ASCII,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                var request = new StringBuilder();
                while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
                {
                    request.AppendLine(line);
                    if (line.Length == 0)
                    {
                        break;
                    }
                }

                Requests.Add(request.ToString());
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response)).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException or IOException)
        {
        }
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wdm-integration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
