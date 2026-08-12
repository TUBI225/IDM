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

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

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
        await using var host = new DownloadHost(services);

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
                    await host.RunPendingAsync(cancellation.Token).ConfigureAwait(false);
                    return 0;
                case "cancel":
                    if (args.Length < 2 || !Guid.TryParse(args[1], out var cancelId))
                    {
                        return 2;
                    }

                    await host.CancelAsync(cancelId, cancellation.Token).ConfigureAwait(false);
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

    private static void PrintUsage()
    {
        Console.WriteLine("idm add <url> <destination>");
        Console.WriteLine("idm run");
        Console.WriteLine("idm cancel <id>");
        Console.WriteLine("Environnement : IDM_DB définit le chemin de la base SQLite.");
    }
}
