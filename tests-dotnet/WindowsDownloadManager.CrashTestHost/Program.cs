using System.Diagnostics;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Persistence.Sqlite;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.CrashTestHost;

internal static class Program
{
    private static readonly byte[] SmallContent = "hello"u8.ToArray();
    private static readonly byte[] LargeContent = CreateLargeContent();

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 4 ||
            !Enum.TryParse<CrashBoundary>(args[0], ignoreCase: false, out var boundary) ||
            !Guid.TryParse(args[1], out var taskId))
        {
            return 2;
        }

        var databasePath = Path.GetFullPath(args[2]);
        var temporaryPath = Path.GetFullPath(args[3]);
        var content = IsSecondBlockBoundary(boundary) ? LargeContent : SmallContent;
        var targetOperation = IsSecondBlockBoundary(boundary) ? 2 : 1;
        var destinationPath = Path.Combine(
            Path.GetDirectoryName(temporaryPath) ?? throw new InvalidDataException(),
            "fixture.bin");
        await using var innerRepository = new SqliteDownloadRepository(databasePath);
        IDownloadRepository repository = IsCheckpointBoundary(boundary)
            ? new TerminatingRepository(innerRepository, boundary, targetOperation)
            : IsFinalizationRepositoryBoundary(boundary)
                ? new TerminatingFinalizationRepository(innerRepository, boundary)
                : innerRepository;
        ITemporaryFileWriter writer = IsWriterBoundary(boundary)
            ? new TerminatingWriter(
                new DurableTemporaryFileWriter(),
                targetOperation,
                IsBeforeWriteAndFlushBoundary(boundary))
            : new DurableTemporaryFileWriter();
        var orchestrator = new DownloadOrchestrator(
            new StubAnalyzer(content),
            new StubContentSource(content),
            writer,
            repository);
        var task = new DownloadTask(
            taskId,
            new Uri("https://example.test/fixture.bin"),
            destinationPath);

        await orchestrator.RunNewAsync(task, temporaryPath, CancellationToken.None);
        if (IsFinalizationBoundary(boundary))
        {
            ITemporaryFileFinalizer finalizer = boundary == CrashBoundary.AfterFinalMove
                ? new TerminatingFinalizer(new AtomicTemporaryFileFinalizer())
                : new AtomicTemporaryFileFinalizer();
            var finalization = new DownloadFinalizationCoordinator(
                new ReadOnlyTemporaryFileInspector(),
                finalizer,
                repository);
            await finalization.FinalizeAsync(task, CancellationToken.None);
        }

        return 3;
    }

    private static void TerminateAbruptly()
    {
        Process.GetCurrentProcess().Kill(entireProcessTree: false);
        Thread.Sleep(Timeout.Infinite);
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

    private static bool IsSecondBlockBoundary(CrashBoundary boundary) =>
        boundary is CrashBoundary.BeforeSecondBlockWriteAndFlush or
            CrashBoundary.AfterSecondBlockDurableFlush or
            CrashBoundary.BeforeSecondCheckpointCommit or
            CrashBoundary.AfterSecondCheckpointCommit;

    private static bool IsFinalizationBoundary(CrashBoundary boundary) =>
        boundary is CrashBoundary.AfterFinalizingCommit or
            CrashBoundary.AfterFinalMove or
            CrashBoundary.AfterCompletedCommit;

    private static bool IsFinalizationRepositoryBoundary(CrashBoundary boundary) =>
        boundary is CrashBoundary.AfterFinalizingCommit or
            CrashBoundary.AfterCompletedCommit;

    private static bool IsWriterBoundary(CrashBoundary boundary) =>
        boundary is CrashBoundary.AfterDurableFlush or
            CrashBoundary.BeforeSecondBlockWriteAndFlush or
            CrashBoundary.AfterSecondBlockDurableFlush;

    private static bool IsBeforeWriteAndFlushBoundary(CrashBoundary boundary) =>
        boundary == CrashBoundary.BeforeSecondBlockWriteAndFlush;

    private static bool IsCheckpointBoundary(CrashBoundary boundary) =>
        boundary is CrashBoundary.BeforeCheckpointCommit or
            CrashBoundary.AfterCheckpointCommit or
            CrashBoundary.BeforeSecondCheckpointCommit or
            CrashBoundary.AfterSecondCheckpointCommit;

    private static bool IsBeforeCheckpointCommit(CrashBoundary boundary) =>
        boundary is CrashBoundary.BeforeCheckpointCommit or
            CrashBoundary.BeforeSecondCheckpointCommit;

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

    private sealed class TerminatingWriter(
        ITemporaryFileWriter inner,
        int targetWrite,
        bool terminateBeforeWriteAndFlush) : ITemporaryFileWriter
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
            if (_writeCount == targetWrite && terminateBeforeWriteAndFlush)
            {
                TerminateAbruptly();
            }

            var boundary = await inner
                .WriteAndFlushAsync(temporaryPath, offset, content, cancellationToken)
                .ConfigureAwait(false);
            if (_writeCount == targetWrite)
            {
                TerminateAbruptly();
            }

            return boundary;
        }
    }

    private sealed class TerminatingRepository(
        IDownloadRepository inner,
        CrashBoundary boundary,
        int targetPositiveCheckpoint) : IDownloadRepository
    {
        private int _positiveCheckpointCount;

        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(id, cancellationToken);

        public async ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            var isPositiveCheckpoint = task.State == DownloadState.Downloading &&
                task.ConfirmedBytes > 0;
            if (!isPositiveCheckpoint)
            {
                await inner.SaveAsync(task, cancellationToken).ConfigureAwait(false);
                return;
            }

            _positiveCheckpointCount++;
            if (_positiveCheckpointCount != targetPositiveCheckpoint)
            {
                await inner.SaveAsync(task, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (IsBeforeCheckpointCommit(boundary))
            {
                TerminateAbruptly();
            }

            await inner.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            TerminateAbruptly();
        }
    }

    private sealed class TerminatingFinalizationRepository(
        IDownloadRepository inner,
        CrashBoundary boundary) : IDownloadRepository
    {
        public ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(id, cancellationToken);

        public async ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            await inner.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            if ((boundary == CrashBoundary.AfterFinalizingCommit &&
                 task.State == DownloadState.Finalizing) ||
                (boundary == CrashBoundary.AfterCompletedCommit &&
                 task.State == DownloadState.Completed))
            {
                TerminateAbruptly();
            }
        }
    }

    private sealed class TerminatingFinalizer(ITemporaryFileFinalizer inner) : ITemporaryFileFinalizer
    {
        public async ValueTask MoveAtomicallyAsync(
            string temporaryPath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            await inner
                .MoveAtomicallyAsync(temporaryPath, destinationPath, cancellationToken)
                .ConfigureAwait(false);
            TerminateAbruptly();
        }
    }

    private enum CrashBoundary
    {
        AfterDurableFlush,
        BeforeCheckpointCommit,
        AfterCheckpointCommit,
        BeforeSecondBlockWriteAndFlush,
        AfterSecondBlockDurableFlush,
        BeforeSecondCheckpointCommit,
        AfterSecondCheckpointCommit,
        AfterFinalizingCommit,
        AfterFinalMove,
        AfterCompletedCommit,
    }
}
