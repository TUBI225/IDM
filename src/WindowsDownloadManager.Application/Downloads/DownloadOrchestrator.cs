using System.Buffers;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class DownloadOrchestrator
{
    private const int BufferSize = 64 * 1024;
    private readonly IRemoteResourceAnalyzer _resourceAnalyzer;
    private readonly IRemoteContentSource _contentSource;
    private readonly ITemporaryFileWriter _temporaryFileWriter;
    private readonly IDownloadRepository _downloadRepository;
    private readonly StartupRecoveryCoordinator? _recoveryCoordinator;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public DownloadOrchestrator(
        IRemoteResourceAnalyzer resourceAnalyzer,
        IRemoteContentSource contentSource,
        ITemporaryFileWriter temporaryFileWriter,
        IDownloadRepository downloadRepository,
        StartupRecoveryCoordinator? recoveryCoordinator = null)
    {
        _resourceAnalyzer = resourceAnalyzer ?? throw new ArgumentNullException(nameof(resourceAnalyzer));
        _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
        _temporaryFileWriter = temporaryFileWriter ?? throw new ArgumentNullException(nameof(temporaryFileWriter));
        _downloadRepository = downloadRepository ?? throw new ArgumentNullException(nameof(downloadRepository));
        _recoveryCoordinator = recoveryCoordinator;
    }

    public async ValueTask<DownloadResumeResult> ResumeAsync(
        DownloadTask task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.State != DownloadState.Downloading)
        {
            throw new InvalidOperationException("Only a download persisted in Downloading state can be resumed.");
        }

        var recoveryCoordinator = _recoveryCoordinator ??
            throw new InvalidOperationException("This orchestrator has no recovery coordinator.");

        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var assessment = await recoveryCoordinator
                .CoordinateAsync(task, cancellationToken)
                .ConfigureAwait(false);
            if (assessment.Status is not (
                StartupRecoveryAssessmentStatus.OverlapMatched or
                StartupRecoveryAssessmentStatus.OverlapNotRequired))
            {
                return new DownloadResumeResult(
                    task.Id,
                    DownloadResumeStatus.Blocked,
                    task.ConfirmedBytes,
                    task.State,
                    assessment);
            }

            var identity = assessment.RemoteIdentity?.ObservedIdentity ??
                throw new InvalidDataException("A resumable assessment must contain the observed remote identity.");
            var temporaryPath = task.TemporaryPath ??
                throw new InvalidDataException("A resumable task must contain a temporary path.");
            var resource = new RemoteResourceInfo(
                task.OriginalUri,
                identity.FinalUri,
                identity.Length,
                SuggestedFileName: null,
                ContentType: null,
                identity.EntityTag,
                identity.LastModified,
                identity.SupportsByteRanges);

            if (identity.Length is null || task.ConfirmedBytes < identity.Length.Value)
            {
                await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
            }

            await SaveAndTransitionAsync(task, DownloadState.Verifying, cancellationToken).ConfigureAwait(false);
            return new DownloadResumeResult(
                task.Id,
                DownloadResumeStatus.ResumedToVerification,
                task.ConfirmedBytes,
                task.State,
                assessment);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async ValueTask<DownloadRunResult> RunNewAsync(
        DownloadTask task,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        if (task.State != DownloadState.New || task.ConfirmedBytes != 0)
        {
            throw new InvalidOperationException("Only a new download with no confirmed bytes can use this operation.");
        }

        if (!Path.IsPathFullyQualified(temporaryPath))
        {
            throw new ArgumentException("The temporary path must be absolute.", nameof(temporaryPath));
        }

        if (string.Equals(temporaryPath, task.DestinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The temporary path must differ from the destination path.", nameof(temporaryPath));
        }

        await SaveAndTransitionAsync(task, DownloadState.Analyzing, cancellationToken).ConfigureAwait(false);
        var resource = await _resourceAnalyzer.AnalyzeAsync(task.OriginalUri, cancellationToken)
            .ConfigureAwait(false);

        task.TransitionTo(DownloadState.Preparing);
        task.RecordPreparation(temporaryPath, ToRemoteIdentity(resource));
        await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
        await _temporaryFileWriter.PrepareNewAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
        await SaveAndTransitionAsync(task, DownloadState.Waiting, cancellationToken).ConfigureAwait(false);
        await SaveAndTransitionAsync(task, DownloadState.Downloading, cancellationToken).ConfigureAwait(false);

        if (resource.Length != 0)
        {
            await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
        }

        await SaveAndTransitionAsync(task, DownloadState.Verifying, cancellationToken).ConfigureAwait(false);
        return new DownloadRunResult(task.Id, temporaryPath, task.ConfirmedBytes, task.State, resource);
    }

    private async ValueTask TransferAsync(
        DownloadTask task,
        string temporaryPath,
        RemoteResourceInfo resource,
        CancellationToken cancellationToken)
    {
        await using var remoteContent = await _contentSource.OpenReadAsync(
            resource,
            task.ConfirmedBytes,
            cancellationToken)
            .ConfigureAwait(false);
        if (resource.Length is { } analyzedLength &&
            remoteContent.TotalLength is { } transferLength &&
            analyzedLength != transferLength)
        {
            throw new InvalidDataException("The remote resource length changed after analysis.");
        }

        var expectedLength = resource.Length ?? remoteContent.TotalLength;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (true)
            {
                var read = await remoteContent.Content.ReadAsync(
                    buffer.AsMemory(0, BufferSize),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                var nextBoundary = checked(task.ConfirmedBytes + read);
                if (expectedLength is { } length && nextBoundary > length)
                {
                    throw new InvalidDataException("The remote resource exceeded its announced length.");
                }

                var flushedBoundary = await _temporaryFileWriter.WriteAndFlushAsync(
                    temporaryPath,
                    task.ConfirmedBytes,
                    buffer.AsMemory(0, read),
                    cancellationToken).ConfigureAwait(false);
                if (flushedBoundary != nextBoundary)
                {
                    throw new InvalidDataException("The temporary writer confirmed an unexpected byte boundary.");
                }

                task.ConfirmPersistedBytes(flushedBoundary);
                await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (expectedLength is { } finalLength && task.ConfirmedBytes != finalLength)
        {
            throw new EndOfStreamException("The remote resource ended before its announced length.");
        }
    }

    private async ValueTask SaveAndTransitionAsync(
        DownloadTask task,
        DownloadState nextState,
        CancellationToken cancellationToken)
    {
        task.TransitionTo(nextState);
        await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
    }

    private static RemoteIdentity ToRemoteIdentity(RemoteResourceInfo resource) =>
        new(
            resource.FinalUri,
            resource.Length,
            resource.EntityTag,
            resource.LastModified,
            resource.SupportsByteRanges);
}
