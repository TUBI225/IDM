using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Abstractions;

public interface IDownloadRepository
{
    ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken);
    ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne toutes les tâches non terminales (hors <c>Completed</c> et <c>Cancelled</c>),
    /// nécessaires au hôte pour reprendre ou réparer au démarrage. Le défaut retourne une liste
    /// vide ; seuls les dépôts persistants qui découvrent des tâches la surchargent.
    /// </summary>
    ValueTask<IReadOnlyList<DownloadTask>> ListNonTerminalAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<DownloadTask>>(Array.Empty<DownloadTask>());
}
