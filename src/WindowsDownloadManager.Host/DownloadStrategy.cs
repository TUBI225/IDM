using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Mode de transfert choisi par la stratégie d'exécution.
/// </summary>
public enum DownloadRunKind
{
    Single,
    Segmented,
    Dynamic,
}

/// <summary>
/// Stratégie pure de choix du mode : longueur inconnue ou nulle, absence de Range ou connexion
/// unique → simple ; Range avec plusieurs connexions et chunks dynamiques → dynamique ; Range avec
/// plusieurs segments statiques → segmenté.
/// </summary>
public static class DownloadStrategy
{
    public static DownloadRunKind Select(RemoteResourceInfo resource, DownloadHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(options);

        if (resource.Length is null or 0 || !resource.SupportsByteRanges)
        {
            return DownloadRunKind.Single;
        }

        if (options.Connections > 1 && options.DynamicChunkSize > 0)
        {
            return DownloadRunKind.Dynamic;
        }

        if (options.Segments > 1)
        {
            return DownloadRunKind.Segmented;
        }

        return DownloadRunKind.Single;
    }
}
