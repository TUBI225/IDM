using System.IO.Pipes;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Client IPC de la CLI (ADR-025) : envoie une commande de contrôle au processus d'exécution via le
/// pipe nommé par utilisateur. Retourne `false` quand aucun serveur n'est joignable (aucun `idm run`
/// actif) ou que la commande est refusée.
/// </summary>
public static class IpcCommandClient
{
    public static async ValueTask<bool> TrySendAsync(
        string command,
        Guid downloadId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var client = new NamedPipeClientStream(
                ".",
                IpcEndpoint.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            await client.ConnectAsync(timeout, cancellationToken).ConfigureAwait(false);
            using (var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true })
            using (var reader = new StreamReader(client, leaveOpen: true))
            {
                await writer.WriteLineAsync($"{command} {downloadId}").ConfigureAwait(false);
                var response = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                return string.Equals(response, "OK", StringComparison.Ordinal);
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
