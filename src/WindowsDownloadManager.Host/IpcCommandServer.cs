using System.IO.Pipes;

namespace WindowsDownloadManager.Host;

/// <summary>
/// Serveur de commandes du processus d'exécution (ADR-025) : écoute le pipe nommé par utilisateur
/// et exécute les commandes `CANCEL <id>` et `PAUSE <id>` reçues depuis la CLI, via les délégations
/// fournies. Les réponses sont `OK` ou `ERR` ; aucune donnée sensible ne transite.
/// </summary>
public sealed class IpcCommandServer : IAsyncDisposable
{
    private const int MaxServerInstances = 16;
    private readonly Func<Guid, CancellationToken, ValueTask> _cancelHandler;
    private readonly Func<Guid, CancellationToken, ValueTask> _pauseHandler;
    private readonly CancellationToken _shutdown;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _listener;

    public IpcCommandServer(
        Func<Guid, CancellationToken, ValueTask> cancelHandler,
        Func<Guid, CancellationToken, ValueTask> pauseHandler,
        CancellationToken shutdown = default)
    {
        ArgumentNullException.ThrowIfNull(cancelHandler);
        ArgumentNullException.ThrowIfNull(pauseHandler);
        _cancelHandler = cancelHandler;
        _pauseHandler = pauseHandler;
        _shutdown = shutdown;
        _listener = Task.Run(async () => await ListenAsync().ConfigureAwait(false));
    }

    private async Task ListenAsync()
    {
        while (!_stop.IsCancellationRequested && !_shutdown.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(
                IpcEndpoint.PipeName,
                PipeDirection.InOut,
                MaxServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0);
            try
            {
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (ObjectDisposedException)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (IOException)
            {
                // Client déconnecté pendant la création du pipe : on libère et on continue.
                await server.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            _ = HandleClientAsync(server);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream server)
    {
        try
        {
            await using var _ = server;
            using var reader = new StreamReader(server, leaveOpen: true);
            using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && Guid.TryParse(parts[1], out var id))
            {
                try
                {
                    if (parts[0] == "CANCEL")
                    {
                        await _cancelHandler(id, _stop.Token).ConfigureAwait(false);
                    }
                    else if (parts[0] == "PAUSE")
                    {
                        await _pauseHandler(id, _stop.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteLineAsync("ERR Commande inconnue.").ConfigureAwait(false);
                        return;
                    }

                    await writer.WriteLineAsync("OK").ConfigureAwait(false);
                    return;
                }
                catch (Exception)
                {
                    await writer.WriteLineAsync("ERR La commande n'a pas pu être exécutée.").ConfigureAwait(false);
                    return;
                }
            }

            await writer.WriteLineAsync("ERR Commande invalide.").ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Client déconnecté en cours de traitement.
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        try
        {
            await _listener.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
