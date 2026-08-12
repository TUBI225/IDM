using WindowsDownloadManager.Network.Http;
using WindowsDownloadManager.Network.Security;
using WindowsDownloadManager.Persistence.Sqlite;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Host;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 2;
        }

        var command = args[0].ToLowerInvariant();
        if (command is "cancel" or "pause")
        {
            if (args.Length < 2 || !Guid.TryParse(args[1], out var controlId))
            {
                return 2;
            }

            return await RunControlCommandAsync(command, controlId).ConfigureAwait(false);
        }

        var singleInstanceName = $"Local\\IDM-DownloadManager-{Environment.UserName}";
        using var singleInstance = new Mutex(initiallyOwned: true, singleInstanceName, out var singleInstanceAcquired);
        if (!singleInstanceAcquired)
        {
            Console.Error.WriteLine("Another idm instance is already running for this user.");
            return 3;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        await using var host = CreateHost();

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "add":
                    if (args.Length < 3)
                    {
                        return 2;
                    }

                    var task = await host.AddAsync(
                        new Uri(args[1]),
                        args[2],
                        cancellationToken: cancellation.Token).ConfigureAwait(false);
                    Console.WriteLine(task.Id);
                    return 0;
                case "run":
                    await using (var server = new IpcCommandServer(
                        (id, token) => host.CancelAsync(id, token),
                        (id, token) => host.PauseAsync(id, token),
                        cancellation.Token))
                    {
                        await host.RunPendingAsync(cancellation.Token).ConfigureAwait(false);
                    }

                    return 0;
                default:
                    PrintUsage();
                    return 2;
            }
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunControlCommandAsync(string command, Guid id)
    {
        if (await IpcCommandClient.TrySendAsync(
                command,
                id,
                TimeSpan.FromSeconds(2),
                CancellationToken.None).ConfigureAwait(false))
        {
            return 0;
        }

        // Aucun processus d'exécution joignable : la commande s'applique directement si l'instance
        // unique est libre, sinon une exécution en cours ne répond pas sur le canal de contrôle.
        var singleInstanceName = $"Local\\IDM-DownloadManager-{Environment.UserName}";
        using var singleInstance = new Mutex(initiallyOwned: true, singleInstanceName, out var acquired);
        if (!acquired)
        {
            Console.Error.WriteLine("Une instance d'exécution est active mais ne répond pas.");
            return 3;
        }

        await using var host = CreateHost();
        try
        {
            if (command == "cancel")
            {
                await host.CancelAsync(id, CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await host.PauseAsync(id, CancellationToken.None).ConfigureAwait(false);
            }

            return 0;
        }
        catch (KeyNotFoundException)
        {
            Console.Error.WriteLine($"Aucun téléchargement avec l'identifiant {id}.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static DownloadHost CreateHost()
    {
        var databasePath = Path.GetFullPath(
            Environment.GetEnvironmentVariable("IDM_DB") ?? Path.Combine(Environment.CurrentDirectory, "idm.db"));
        var httpClient = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new PublicNetworkAddressPolicy());
        var uriValidator = new PublicHttpUriSafetyValidator();
        var contentSource = new HttpRemoteContentSource(httpClient, uriValidator);
        var services = new DownloadHostServices(
            new HttpRemoteResourceAnalyzer(httpClient, uriValidator),
            contentSource,
            new DurableTemporaryFileWriter(),
            new ReadOnlyTemporaryFileInspector(),
            new ReadOnlyTemporaryFileRangeReader(),
            contentSource,
            new Sha256TemporaryFileHasher(),
            new AtomicTemporaryFileFinalizer(),
            new SqliteDownloadRepository(databasePath));
        return new DownloadHost(services);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("idm add <url> <destination>");
        Console.WriteLine("idm run");
        Console.WriteLine("idm cancel <id>");
        Console.WriteLine("idm pause <id>");
        Console.WriteLine("Environnement : IDM_DB définit le chemin de la base SQLite.");
    }
}
