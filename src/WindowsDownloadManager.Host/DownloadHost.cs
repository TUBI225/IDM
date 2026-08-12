using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Application.RateLimiting;
using WindowsDownloadManager.Application.Retries;
using WindowsDownloadManager.Application.Scheduling;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Processus hôte headless (ADR-025) : unique propriétaire logique du dépôt, des fichiers et du
/// scheduler. Exécute le cycle complet — ajout, stratégie simple/segmenté/dynamique, vérification,
/// finalisation, reprise au checkpoint, décision des sept niveaux et retransmission contrôlée —
/// via les ports injectés. Le contrôle de débit (`BandwidthController`) est appliqué par un décorateur
/// de flux ; le scheduler arbitre les priorités et la concurrence globale.
/// </summary>
public sealed class DownloadHost : IAsyncDisposable
{
    private readonly DownloadHostServices _services;
    private readonly DownloadHostOptions _options;
    private readonly DownloadScheduler _scheduler;
    private readonly StartupRecoveryCoordinator _recovery;
    private readonly DownloadFinalizationCoordinator _finalization;
    private readonly ForcedResumeEngine _resumeEngine;
    private readonly ControlledRetransmissionEngine _retransmission;
    private readonly BandwidthController? _bandwidth;
    private readonly IRetryPolicy? _retryPolicy;
    private readonly Func<DownloadTask, string> _temporaryPathFactory;
    private readonly HashSet<Guid> _submittedIds = [];
    private bool _disposed;

    public DownloadHost(
        DownloadHostServices services,
        DownloadHostOptions? options = null,
        BandwidthController? bandwidth = null,
        IRetryPolicy? retryPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        _options = options ?? new DownloadHostOptions();
        ValidateOptions(_options);
        _scheduler = new DownloadScheduler(
            _options.MaxConcurrentDownloads,
            _options.AgingInterval,
            _options.AgingBoost);
        _recovery = new StartupRecoveryCoordinator(
            new StartupRecoveryReconciler(services.FileInspector),
            new RemoteIdentityReconciler(services.ResourceAnalyzer),
            new RecoveryDecisionEvaluator(),
            new RecoveryOverlapVerifier(services.TemporaryFileRangeReader, services.RangeReader));
        _finalization = new DownloadFinalizationCoordinator(
            services.FileInspector,
            services.FileHasher,
            services.FileFinalizer,
            services.Repository);
        _resumeEngine = new ForcedResumeEngine();
        _retransmission = new ControlledRetransmissionEngine();
        _bandwidth = bandwidth;
        _retryPolicy = retryPolicy;
        _temporaryPathFactory = _options.TemporaryPathFactory ?? DefaultTemporaryPath;
    }

    public async ValueTask<DownloadTask> AddAsync(
        Uri uri,
        string destinationPath,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var task = new DownloadTask(Guid.NewGuid(), uri, Path.GetFullPath(destinationPath));
        Submit(task, priority);
        await _services.Repository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Soumet au scheduler toutes les tâches non terminales persistées (reprise au démarrage).
    /// Idempotente : un identifiant déjà soumis n'est jamais soumis deux fois.
    /// </summary>
    public async ValueTask RebuildScheduleAsync(CancellationToken cancellationToken)
    {
        var tasks = await _services.Repository
            .ListNonTerminalAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var task in tasks)
        {
            Submit(task, priority: 0);
        }
    }

    public async ValueTask<DownloadTask?> RunOnceAsync(CancellationToken cancellationToken)
    {
        var scheduled = _scheduler.AcquireNext(DateTimeOffset.UtcNow);
        if (scheduled is null)
        {
            return null;
        }

        try
        {
            var task = await _services.Repository
                .FindAsync(scheduled.DownloadId, cancellationToken)
                .ConfigureAwait(false);
            if (task is null)
            {
                return null;
            }

            await ExecuteTaskAsync(task, cancellationToken).ConfigureAwait(false);
            return task;
        }
        finally
        {
            _scheduler.Release(scheduled.DownloadId);
        }
    }

    public async ValueTask<int> RunPendingAsync(CancellationToken cancellationToken)
    {
        await RebuildScheduleAsync(cancellationToken).ConfigureAwait(false);
        var count = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = await RunOnceAsync(cancellationToken).ConfigureAwait(false);
            if (task is null)
            {
                return count;
            }

            count++;
        }
    }

    public async ValueTask CancelAsync(Guid downloadId, CancellationToken cancellationToken)
    {
        var task = await RequireTaskAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (!DownloadStateMachine.CanTransition(task.State, DownloadState.Cancelled))
        {
            throw new InvalidOperationException(
                $"The download cannot be cancelled from state {task.State}.");
        }

        await SaveAndTransitionAsync(task, DownloadState.Cancelled, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask PauseAsync(Guid downloadId, CancellationToken cancellationToken)
    {
        var task = await RequireTaskAsync(downloadId, cancellationToken).ConfigureAwait(false);
        if (!DownloadStateMachine.CanTransition(task.State, DownloadState.PauseRequested))
        {
            throw new InvalidOperationException(
                $"The download cannot be paused from state {task.State}.");
        }

        await SaveAndTransitionAsync(task, DownloadState.PauseRequested, cancellationToken).ConfigureAwait(false);
        await SaveAndTransitionAsync(task, DownloadState.Paused, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ExecuteTaskAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        switch (task.State)
        {
            case DownloadState.New:
                await RunNewAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            case DownloadState.Downloading:
                await RunResumeAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            case DownloadState.Verifying:
                await _finalization.FinalizeAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            case DownloadState.Finalizing:
                await _finalization.RepairAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            default:
                break;
        }
    }

    private async ValueTask RunNewAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        var temporaryPath = _temporaryPathFactory(task);
        var resource = await _services.ResourceAnalyzer
            .AnalyzeAsync(task.OriginalUri, cancellationToken)
            .ConfigureAwait(false);
        var orchestrator = CreateOrchestrator(task, resource);
        var kind = DownloadStrategy.Select(resource, _options);
        switch (kind)
        {
            case DownloadRunKind.Segmented:
                await orchestrator
                    .RunSegmentedAsync(task, temporaryPath, _options.Segments, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case DownloadRunKind.Dynamic:
                await orchestrator
                    .RunDynamicSegmentedAsync(
                        task,
                        temporaryPath,
                        _options.Connections,
                        _options.DynamicChunkSize,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                await orchestrator
                    .RunNewAsync(task, temporaryPath, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        if (task.State == DownloadState.Verifying)
        {
            await _finalization.FinalizeAsync(task, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask RunResumeAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        if (task.RemoteIdentity is null)
        {
            // Métadonnées absentes : aucune reprise native ni retransmission bornée possible.
            // Arrêt sûr sans mutation : la tâche reste non terminale pour une décision manuelle.
            return;
        }

        var resource = BuildResumeResource(task);
        var orchestrator = CreateOrchestrator(task, resource);
        var result = await orchestrator
            .ResumeAsync(task, cancellationToken)
            .ConfigureAwait(false);
        if (result.Status == DownloadResumeStatus.ResumedToVerification)
        {
            await _finalization.FinalizeAsync(task, cancellationToken).ConfigureAwait(false);
            return;
        }

        var decision = _resumeEngine.Evaluate(BuildResumeContext(task, result.Assessment));
        switch (decision.Level)
        {
            case ForcedResumeLevel.Retransmission:
                await RunRetransmissionAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            default:
                if (decision.TargetState is { } targetState)
                {
                    await SaveAndTransitionAsync(task, targetState, cancellationToken)
                        .ConfigureAwait(false);
                }
                break;
        }
    }

    private async ValueTask RunRetransmissionAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        var identity = task.RemoteIdentity
            ?? throw new InvalidDataException("A retransmission requires a persisted remote identity.");
        var temporaryPath = task.TemporaryPath
            ?? throw new InvalidDataException("A retransmission requires a temporary path.");

        var cost = _retransmission.EstimateCost(identity.Length, task.ConfirmedBytes);
        if (cost.RequiresConsent && !_options.AllowRetransmissionWithoutConsent)
        {
            // Retransmission coûteuse non consentie : arrêt sûr sans mutation ;
            // la tâche reste non terminale pour une décision manuelle.
            return;
        }

        var resource = BuildRetransmissionResource(task);
        await using var lease = await _services.ContentSource
            .OpenReadAsync(resource, 0, cancellationToken)
            .ConfigureAwait(false);
        var result = await _retransmission
            .ExecuteAsync(
                task.Id,
                lease.Content,
                identity.Length,
                temporaryPath,
                _services.TemporaryFileRangeReader,
                _services.TemporaryFileWriter,
                cancellationToken)
            .ConfigureAwait(false);

        switch (result.Status)
        {
            case ControlledRetransmissionStatus.Completed:
                task.ConfirmPersistedBytes(result.BytesAlreadyLocal);
                await _services.Repository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
                await SaveAndTransitionAsync(task, DownloadState.Verifying, cancellationToken)
                    .ConfigureAwait(false);
                await _finalization.FinalizeAsync(task, cancellationToken).ConfigureAwait(false);
                break;
            case ControlledRetransmissionStatus.DivergenceDetected:
                await SaveAndTransitionAsync(
                    task,
                    DownloadState.RemoteFileChanged,
                    cancellationToken).ConfigureAwait(false);
                break;
            default:
                await SaveAndTransitionAsync(
                    task,
                    DownloadState.PermanentFailure,
                    cancellationToken).ConfigureAwait(false);
                break;
        }
    }


    private DownloadOrchestrator CreateOrchestrator(DownloadTask task, RemoteResourceInfo resource)
    {
        IRemoteContentSource contentSource = _services.ContentSource;
        if (_bandwidth is not null)
        {
            var domain = resource.FinalUri.Host;
            var controller = _bandwidth;
            contentSource = new ThrottledRemoteContentSource(
                contentSource,
                async (byteCount, cancellationToken) => await controller
                    .AcquireAsync(task.Id, domain, byteCount, cancellationToken)
                    .ConfigureAwait(false));
        }

        return new DownloadOrchestrator(
            _services.ResourceAnalyzer,
            contentSource,
            _services.TemporaryFileWriter,
            _services.Repository,
            _recovery,
            _retryPolicy);
    }

    private async ValueTask<DownloadTask> RequireTaskAsync(
        Guid downloadId,
        CancellationToken cancellationToken)
    {
        var task = await _services.Repository
            .FindAsync(downloadId, cancellationToken)
            .ConfigureAwait(false);
        if (task is null)
        {
            throw new KeyNotFoundException($"No download exists with id {downloadId}.");
        }

        return task;
    }

    private void Submit(DownloadTask task, int priority)
    {
        if (_submittedIds.Add(task.Id))
        {
            _scheduler.Submit(new ScheduledDownload(task.Id, priority, DateTimeOffset.UtcNow));
        }
    }

    private static ForcedResumeContext BuildResumeContext(
        DownloadTask task,
        StartupRecoveryAssessmentResult assessment)
    {
        var identity = assessment.RemoteIdentity;
        return new ForcedResumeContext(
            task.Id,
            task.ConfirmedBytes,
            ResumeMetadataPresent: task.TemporaryPath is not null && task.RemoteIdentity is not null,
            RemoteIdentityCompatible:
                identity?.Status == RemoteIdentityReconciliationStatus.Compatible,
            IdentityContradicted:
                identity?.Status == RemoteIdentityReconciliationStatus.Contradictory,
            IdentityEvidenceInsufficient:
                identity?.Status == RemoteIdentityReconciliationStatus.InsufficientEvidence,
            ByteRangeSupportObserved: identity?.ObservedIdentity?.SupportsByteRanges == true,
            ByteRangeSupportLost:
                identity?.Status == RemoteIdentityReconciliationStatus.ResumeCapabilityLost,
            FinalUrlChangedOnly: false,
            LinkExpired: false,
            NewLinkProvided: false,
            RecoveryNeeded:
                (assessment.ReconciliationBlockers & LocalRecoveryBlockers) != RecoveryBlocker.None,
            UserRequestsSafeStop: false);
    }

    private const RecoveryBlocker LocalRecoveryBlockers =
        RecoveryBlocker.RecoveryMetadataAbsent |
        RecoveryBlocker.TemporaryFileAbsent |
        RecoveryBlocker.CheckpointAheadOfTemporaryFile |
        RecoveryBlocker.UnconfirmedTemporaryFileTail;

    private static RemoteResourceInfo BuildResumeResource(DownloadTask task)
    {
        var identity = task.RemoteIdentity
            ?? throw new InvalidDataException("A resumable task must contain a remote identity.");
        return new RemoteResourceInfo(
            task.OriginalUri,
            identity.FinalUri,
            identity.Length,
            SuggestedFileName: null,
            ContentType: null,
            identity.EntityTag,
            identity.LastModified,
            identity.SupportsByteRanges,
            identity.Sha256);
    }

    private static RemoteResourceInfo BuildRetransmissionResource(DownloadTask task)
    {
        var identity = task.RemoteIdentity
            ?? throw new InvalidDataException("A retransmission requires a persisted remote identity.");
        return new RemoteResourceInfo(
            task.OriginalUri,
            identity.FinalUri,
            identity.Length,
            SuggestedFileName: null,
            ContentType: null,
            identity.EntityTag,
            identity.LastModified,
            SupportsByteRanges: false,
            identity.Sha256);
    }

    private async ValueTask SaveAndTransitionAsync(
        DownloadTask task,
        DownloadState nextState,
        CancellationToken cancellationToken)
    {
        if (!DownloadStateMachine.CanTransition(task.State, nextState) &&
            DownloadStateMachine.CanTransition(task.State, DownloadState.Reconnecting) &&
            DownloadStateMachine.CanTransition(DownloadState.Reconnecting, DownloadState.TestingResume) &&
            DownloadStateMachine.CanTransition(DownloadState.TestingResume, nextState))
        {
            task.TransitionTo(DownloadState.Reconnecting);
            task.TransitionTo(DownloadState.TestingResume);
        }

        task.TransitionTo(nextState);
        await _services.Repository.SaveAsync(task, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateOptions(DownloadHostOptions options)
    {
        if (options.Connections <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Connections must be positive.");
        }

        if (options.Segments <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Segments must be positive.");
        }

        if (options.DynamicChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The dynamic chunk size must be positive.");
        }

        if (options.MaxConcurrentDownloads <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxConcurrentDownloads must be positive.");
        }

        if (options.AgingBoost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AgingBoost cannot be negative.");
        }
    }

    private static string DefaultTemporaryPath(DownloadTask task)
    {
        var directory = Path.GetDirectoryName(task.DestinationPath)
            ?? throw new InvalidDataException("The destination path has no parent directory.");
        var fileName = Path.GetFileName(task.DestinationPath);
        return Path.Combine(directory, $".{fileName}.wdm-partial");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_services.Repository is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}

