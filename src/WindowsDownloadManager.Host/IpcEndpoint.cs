namespace WindowsDownloadManager.Host;

/// <summary>
/// Point d'accès IPC du processus d'exécution (ADR-025) : nom de pipe porté par l'utilisateur
/// courant. L'accès au pipe est hérité de la DACL du processus créateur, donc restreint au compte
/// qui lance `idm run`. Aucun secret ne transite : seuls des GUID de tâche et des accusés.
/// </summary>
public static class IpcEndpoint
{
    public static string PipeName => $"idm-{Environment.UserName}";
}

