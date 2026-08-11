using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Persistence.Sqlite;

namespace WindowsDownloadManager.Persistence.Tests;

[TestClass]
public sealed class SqliteDownloadRepositoryTests
{
    [TestMethod]
    public async Task SaveAndFind_RoundTrip_PreservesRecoverableState()
    {
        using var directory = new TemporaryDirectory();
        await using var repository = new SqliteDownloadRepository(directory.DatabasePath);
        var task = new DownloadTask(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin?token=secret#fragment"),
            "C:\\Downloads\\file.bin");
        task.TransitionTo(DownloadState.Analyzing);
        task.TransitionTo(DownloadState.Preparing);
        task.RecordPreparation(
            "C:\\Downloads\\file.download",
            new RemoteIdentity(
                new Uri("https://cdn.example.test/file.bin?access=secret#fragment"),
                8192,
                "\"strong-v1\"",
                DateTimeOffset.Parse("2026-08-04T00:00:00Z"),
                supportsByteRanges: true));
        task.ConfirmPersistedBytes(4096);

        await repository.SaveAsync(task, CancellationToken.None);
        var restored = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.AreEqual(task.Id, restored.Id);
        Assert.AreEqual(new Uri("https://example.test/file.bin"), restored.OriginalUri);
        Assert.AreEqual(task.DestinationPath, restored.DestinationPath);
        Assert.AreEqual(DownloadState.Preparing, restored.State);
        Assert.AreEqual(4096, restored.ConfirmedBytes);
        Assert.AreEqual("C:\\Downloads\\file.download", restored.TemporaryPath);
        Assert.IsNotNull(restored.RemoteIdentity);
        Assert.AreEqual(new Uri("https://cdn.example.test/file.bin"), restored.RemoteIdentity.FinalUri);
        Assert.AreEqual(8192, restored.RemoteIdentity.Length);
        Assert.AreEqual("\"strong-v1\"", restored.RemoteIdentity.EntityTag);
        Assert.AreEqual(DateTimeOffset.Parse("2026-08-04T00:00:00Z"), restored.RemoteIdentity.LastModified);
        Assert.IsTrue(restored.RemoteIdentity.SupportsByteRanges);

        var databaseBytes = await File.ReadAllBytesAsync(directory.DatabasePath);
        var databaseText = System.Text.Encoding.UTF8.GetString(databaseBytes);
        Assert.IsFalse(databaseText.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Initialize_Twice_RecordsSingleChecksummedMigration()
    {
        using var directory = new TemporaryDirectory();
        await using var repository = new SqliteDownloadRepository(directory.DatabasePath);

        await repository.InitializeAsync(CancellationToken.None);
        await repository.InitializeAsync(CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={directory.DatabasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*), LENGTH(MIN(checksum)) FROM schema_migrations;";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(3L, reader.GetInt64(0));
        Assert.AreEqual(64L, reader.GetInt64(1));
    }

    [TestMethod]
    public async Task Initialize_Version1Database_MigratesWithoutLosingExistingTask()
    {
        using var directory = new TemporaryDirectory();
        var task = new DownloadTask(
            Guid.NewGuid(),
            new Uri("https://example.test/legacy.bin"),
            "C:\\Downloads\\legacy.bin");
        task.TransitionTo(DownloadState.Analyzing);
        task.ConfirmPersistedBytes(123);
        await using (var repository = new SqliteDownloadRepository(directory.DatabasePath))
        {
            await repository.SaveAsync(task, CancellationToken.None);
        }

        await DowngradeToVersion1Async(directory.DatabasePath);

        await using var migratedRepository = new SqliteDownloadRepository(directory.DatabasePath);
        var restored = await migratedRepository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.AreEqual(123, restored.ConfirmedBytes);
        Assert.AreEqual(DownloadState.Analyzing, restored.State);
        Assert.IsNull(restored.TemporaryPath);
        Assert.IsNull(restored.RemoteIdentity);
        Assert.AreEqual(3L, await MigrationCountAsync(directory.DatabasePath));
    }

    [TestMethod]
    public async Task SaveAndFind_FinalizingTask_PreservesVerifiedSha256()
    {
        using var directory = new TemporaryDirectory();
        const string sha256 = "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824";
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Finalizing,
            confirmedBytes: 5,
            "C:\\Downloads\\file.download",
            new RemoteIdentity(
                new Uri("https://example.test/file.bin"),
                5,
                "\"v1\"",
                null,
                supportsByteRanges: true),
            sha256);
        await using var repository = new SqliteDownloadRepository(directory.DatabasePath);

        await repository.SaveAsync(task, CancellationToken.None);
        var restored = await repository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.AreEqual(sha256, restored.VerifiedSha256);
        Assert.AreEqual(DownloadState.Finalizing, restored.State);
    }

    [TestMethod]
    public async Task Initialize_Version2Database_AddsNullableVerifiedSha256()
    {
        using var directory = new TemporaryDirectory();
        var task = new DownloadTask(
            Guid.NewGuid(),
            new Uri("https://example.test/legacy-v2.bin"),
            "C:\\Downloads\\legacy-v2.bin");
        await using (var repository = new SqliteDownloadRepository(directory.DatabasePath))
        {
            await repository.SaveAsync(task, CancellationToken.None);
        }

        await DowngradeToVersion2Async(directory.DatabasePath);

        await using var migratedRepository = new SqliteDownloadRepository(directory.DatabasePath);
        var restored = await migratedRepository.FindAsync(task.Id, CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.IsNull(restored.VerifiedSha256);
        Assert.AreEqual(3L, await MigrationCountAsync(directory.DatabasePath));
    }

    [TestMethod]
    public async Task Find_IncompleteRecoveryMetadata_IsRejected()
    {
        using var directory = new TemporaryDirectory();
        var task = new DownloadTask(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin");
        task.TransitionTo(DownloadState.Analyzing);
        task.TransitionTo(DownloadState.Preparing);
        task.RecordPreparation(
            "C:\\Downloads\\file.download",
            new RemoteIdentity(new Uri("https://example.test/file.bin"), 1, null, null, true));
        await using (var repository = new SqliteDownloadRepository(directory.DatabasePath))
        {
            await repository.SaveAsync(task, CancellationToken.None);
        }

        await using (var connection = new SqliteConnection($"Data Source={directory.DatabasePath};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE downloads SET final_url = NULL WHERE id = $id;";
            command.Parameters.AddWithValue("$id", task.Id.ToString("D"));
            await command.ExecuteNonQueryAsync();
        }

        await using var reopened = new SqliteDownloadRepository(directory.DatabasePath);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await reopened.FindAsync(task.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Find_UnknownId_ReturnsNull()
    {
        using var directory = new TemporaryDirectory();
        await using var repository = new SqliteDownloadRepository(directory.DatabasePath);

        var restored = await repository.FindAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(restored);
    }

    [TestMethod]
    public async Task Initialize_TamperedMigrationChecksum_IsRejected()
    {
        using var directory = new TemporaryDirectory();
        await using (var repository = new SqliteDownloadRepository(directory.DatabasePath))
        {
            await repository.InitializeAsync(CancellationToken.None);
        }

        await using (var connection = new SqliteConnection($"Data Source={directory.DatabasePath};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE schema_migrations SET checksum = 'tampered' WHERE version = 1;";
            await command.ExecuteNonQueryAsync();
        }

        await using var reopened = new SqliteDownloadRepository(directory.DatabasePath);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await reopened.InitializeAsync(CancellationToken.None));
    }

    [TestMethod]
    public void Constructor_RelativeDatabasePath_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new SqliteDownloadRepository("downloads.sqlite3"));
    }

    private static async Task DowngradeToVersion1Async(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM schema_migrations WHERE version >= 2;
            ALTER TABLE downloads RENAME TO downloads_v2;
            CREATE TABLE downloads (
                id TEXT PRIMARY KEY NOT NULL,
                original_url TEXT NOT NULL,
                destination_path TEXT COLLATE NOCASE NOT NULL,
                state INTEGER NOT NULL,
                confirmed_bytes INTEGER NOT NULL DEFAULT 0 CHECK (confirmed_bytes >= 0),
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            INSERT INTO downloads(
                id, original_url, destination_path, state, confirmed_bytes, created_at, updated_at)
            SELECT id, original_url, destination_path, state, confirmed_bytes, created_at, updated_at
            FROM downloads_v2;
            DROP TABLE downloads_v2;
            CREATE UNIQUE INDEX ux_downloads_destination_path ON downloads(destination_path);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DowngradeToVersion2Async(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM schema_migrations WHERE version = 3;
            ALTER TABLE downloads RENAME TO downloads_v3;
            CREATE TABLE downloads (
                id TEXT PRIMARY KEY NOT NULL,
                original_url TEXT NOT NULL,
                destination_path TEXT COLLATE NOCASE NOT NULL,
                state INTEGER NOT NULL,
                confirmed_bytes INTEGER NOT NULL DEFAULT 0 CHECK (confirmed_bytes >= 0),
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                temporary_path TEXT COLLATE NOCASE NULL,
                final_url TEXT NULL,
                total_size INTEGER NULL CHECK (total_size >= 0),
                etag TEXT NULL,
                last_modified TEXT NULL,
                supports_byte_ranges INTEGER NULL CHECK (supports_byte_ranges IN (0, 1))
            );
            INSERT INTO downloads(
                id, original_url, destination_path, state, confirmed_bytes, created_at, updated_at,
                temporary_path, final_url, total_size, etag, last_modified, supports_byte_ranges)
            SELECT id, original_url, destination_path, state, confirmed_bytes, created_at, updated_at,
                   temporary_path, final_url, total_size, etag, last_modified, supports_byte_ranges
            FROM downloads_v3;
            DROP TABLE downloads_v3;
            CREATE UNIQUE INDEX ux_downloads_destination_path ON downloads(destination_path);
            CREATE UNIQUE INDEX ux_downloads_temporary_path
                ON downloads(temporary_path) WHERE temporary_path IS NOT NULL;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> MigrationCountAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM schema_migrations;";
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wdm-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        DatabasePath = System.IO.Path.Combine(Path, "downloads.sqlite3");
    }

    public string Path { get; }
    public string DatabasePath { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
