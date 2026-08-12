using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Ports partagés par le processus hôte. Le `DownloadHost` en est l'unique propriétaire logique :
/// il les assemble dans le scheduler, le coordinateur de récupération, la finalisation, la
/// retransmission et les orchestrateurs par exécution.
/// </summary>
public sealed record DownloadHostServices(
    IRemoteResourceAnalyzer ResourceAnalyzer,
    IRemoteContentSource ContentSource,
    ITemporaryFileWriter TemporaryFileWriter,
    ITemporaryFileInspector FileInspector,
    ITemporaryFileRangeReader TemporaryFileRangeReader,
    IRemoteRangeReader RangeReader,
    ITemporaryFileHasher FileHasher,
    ITemporaryFileFinalizer FileFinalizer,
    IDownloadRepository Repository);

/// <summary>
/// Options d'exécution du processus hôte. `TemporaryPathFactory` détermine le chemin du fichier
/// partiel d'une tâche ; par défaut `{destinationDirectory}\.{fileName}.wdm-partial`.
/// </summary>
public sealed record DownloadHostOptions(
    int Connections = 4,
    int Segments = 4,
    int DynamicChunkSize = 64 * 1024,
    int MaxConcurrentDownloads = 1,
    TimeSpan? AgingInterval = null,
    int AgingBoost = 0,
    bool AllowRetransmissionWithoutConsent = false,
    Func<DownloadTask, string>? TemporaryPathFactory = null);
