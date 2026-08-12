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
/// plusieurs segments statiques → segmenté. La segmentation multi-connexions exige une identité
/// distante forte (ETag fort, Last-Modified ou SHA-256) : sans preuve de stabilité de la ressource,
/// le mode retombe en simple pour ne jamais mélanger des versions différentes d'un même fichier.
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

        if (!HasStrongIdentity(resource))
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

    private static bool HasStrongIdentity(RemoteResourceInfo resource) =>
        IsStrongEntityTag(resource.EntityTag) ||
        resource.LastModified is not null ||
        !string.IsNullOrWhiteSpace(resource.Sha256);

    private static bool IsStrongEntityTag(string? entityTag) =>
        !string.IsNullOrWhiteSpace(entityTag) &&
        !entityTag.StartsWith("W/", StringComparison.OrdinalIgnoreCase);
}
