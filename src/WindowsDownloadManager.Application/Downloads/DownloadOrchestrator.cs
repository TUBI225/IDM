using System.Buffers;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Retries;
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
    private readonly IRetryPolicy? _retryPolicy;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public DownloadOrchestrator(
        IRemoteResourceAnalyzer resourceAnalyzer,
        IRemoteContentSource contentSource,
        ITemporaryFileWriter temporaryFileWriter,
        IDownloadRepository downloadRepository,
        StartupRecoveryCoordinator? recoveryCoordinator = null,
        IRetryPolicy? retryPolicy = null)
    {
        _resourceAnalyzer = resourceAnalyzer ?? throw new ArgumentNullException(nameof(resourceAnalyzer));
        _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
        _temporaryFileWriter = temporaryFileWriter ?? throw new ArgumentNullException(nameof(temporaryFileWriter));
        _downloadRepository = downloadRepository ?? throw new ArgumentNullException(nameof(downloadRepository));
        _recoveryCoordinator = recoveryCoordinator;
        _retryPolicy = retryPolicy;
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
                identity.SupportsByteRanges,
                identity.Sha256);

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

    public async ValueTask<DownloadResumeResult> ResumeSegmentedAsync(
        DownloadTask task,
        int segmentCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (segmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

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
                identity.SupportsByteRanges,
                identity.Sha256);

            if (identity.Length is null)
            {
                await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
            }
            else if (task.ConfirmedBytes < identity.Length.Value)
            {
                if (identity.SupportsByteRanges && segmentCount > 1)
                {
                    var remainingLength = identity.Length.Value - task.ConfirmedBytes;
                    var remainingSegments = SegmentPlanner.Plan(remainingLength, segmentCount)
                        .Select(segment => new DownloadSegment(
                            task.ConfirmedBytes + segment.StartOffset,
                            segment.Length))
                        .ToArray();
                    await SegmentedTransferAsync(task, temporaryPath, resource, remainingSegments, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
                }
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

    public async ValueTask<DownloadRunResult> RunSegmentedAsync(
        DownloadTask task,
        string temporaryPath,
        int segmentCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        if (segmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

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

        if (resource.Length is null)
        {
            // Taille inconnue : repli connexion unique (TransferAsync résout via TotalLength).
            await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
        }
        else if (resource.Length != 0)
        {
            if (resource.SupportsByteRanges && segmentCount > 1)
            {
                var segments = SegmentPlanner.Plan(resource.Length.Value, segmentCount);
                await SegmentedTransferAsync(task, temporaryPath, resource, segments, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await TransferAsync(task, temporaryPath, resource, cancellationToken).ConfigureAwait(false);
            }
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
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await TransferCoreAsync(task, temporaryPath, resource, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            catch (Exception exception)
            {
                if (_retryPolicy is null || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                var decision = _retryPolicy.Evaluate(attempt, exception);
                if (!decision.ShouldRetry)
                {
                    throw;
                }

                await Task.Delay(decision.Delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask TransferCoreAsync(
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

    private async ValueTask SegmentedTransferAsync(
        DownloadTask task,
        string temporaryPath,
        RemoteResourceInfo resource,
        IReadOnlyList<DownloadSegment> segments,
        CancellationToken cancellationToken)
    {
        var totalLength = resource.Length
            ?? throw new InvalidDataException("A segmented transfer requires an announced remote length.");
        var completed = new bool[segments.Count];
        var progressLock = new SemaphoreSlim(1, 1);
        var segmentTasks = new Task[segments.Count];

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var capturedIndex = index;
            segmentTasks[index] = Task.Run(
                () => TransferSegmentAsync(
                        task,
                        temporaryPath,
                        resource,
                        segments,
                        segment,
                        capturedIndex,
                        completed,
                        progressLock,
                        cancellationToken)
                    .AsTask(),
                cancellationToken);
        }

        await Task.WhenAll(segmentTasks).ConfigureAwait(false);

        for (var index = 0; index < completed.Length; index++)
        {
            if (!completed[index])
            {
                throw new InvalidDataException($"The segment {index} did not complete.");
            }
        }

        if (task.ConfirmedBytes != totalLength)
        {
            throw new InvalidDataException("The segmented transfer did not confirm the full remote length.");
        }
    }

    private async ValueTask TransferSegmentAsync(
        DownloadTask task,
        string temporaryPath,
        RemoteResourceInfo resource,
        IReadOnlyList<DownloadSegment> segments,
        DownloadSegment segment,
        int completedIndex,
        bool[] completed,
        SemaphoreSlim progressLock,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await TransferSegmentCoreAsync(
                        task,
                        temporaryPath,
                        resource,
                        segment,
                        progressLock,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
            }
            catch (Exception exception)
            {
                if (_retryPolicy is null || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                var decision = _retryPolicy.Evaluate(attempt, exception);
                if (!decision.ShouldRetry)
                {
                    throw;
                }

                await Task.Delay(decision.Delay, cancellationToken).ConfigureAwait(false);
            }
        }

        await progressLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            completed[completedIndex] = true;
            var contiguousProgress = ComputeContiguousProgress(completed, segments);
            if (contiguousProgress > task.ConfirmedBytes)
            {
                task.ConfirmPersistedBytes(contiguousProgress);
                await _downloadRepository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            progressLock.Release();
        }
    }

    private async ValueTask TransferSegmentCoreAsync(
        DownloadTask task,
        string temporaryPath,
        RemoteResourceInfo resource,
        DownloadSegment segment,
        SemaphoreSlim progressLock,
        CancellationToken cancellationToken)
    {
        await using var remoteContent = _contentSource is IRemoteBoundedContentSource bounded
            ? await bounded.OpenBoundedReadAsync(
                resource,
                segment.StartOffset,
                segment.EndOffsetExclusive - 1,
                cancellationToken).ConfigureAwait(false)
            : await _contentSource
                .OpenReadAsync(resource, segment.StartOffset, cancellationToken)
                .ConfigureAwait(false);

        var remaining = segment.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (remaining > 0)
            {
                var read = await remoteContent.Content.ReadAsync(
                    buffer.AsMemory(0, (int)Math.Min(BufferSize, remaining)),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("The remote resource ended before the segment completed.");
                }

                var segmentOffset = segment.EndOffsetExclusive - remaining;
                var nextBoundary = checked(segmentOffset + read);
                await progressLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                long flushedBoundary;
                try
                {
                    flushedBoundary = await _temporaryFileWriter
                        .WriteAndFlushAsync(temporaryPath, segmentOffset, buffer.AsMemory(0, read), cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    progressLock.Release();
                }

                if (flushedBoundary != nextBoundary)
                {
                    throw new InvalidDataException("The temporary writer confirmed an unexpected byte boundary.");
                }

                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static long ComputeContiguousProgress(bool[] completed, IReadOnlyList<DownloadSegment> segments)
    {
        var progress = 0L;
        for (var index = 0; index < completed.Length; index++)
        {
            if (!completed[index])
            {
                break;
            }

            progress = segments[index].EndOffsetExclusive;
        }

        return progress;
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
            resource.SupportsByteRanges,
            resource.Sha256);
}
