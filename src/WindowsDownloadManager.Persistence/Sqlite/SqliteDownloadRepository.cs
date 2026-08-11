using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Persistence.Sqlite;

public sealed class SqliteDownloadRepository : IDownloadRepository, IAsyncDisposable
{
    private const int CurrentSchemaVersion = 3;
    private const string InitialMigration = """
        CREATE TABLE downloads (
            id TEXT PRIMARY KEY NOT NULL,
            original_url TEXT NOT NULL,
            destination_path TEXT COLLATE NOCASE NOT NULL,
            state INTEGER NOT NULL,
            confirmed_bytes INTEGER NOT NULL DEFAULT 0 CHECK (confirmed_bytes >= 0),
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );
        CREATE UNIQUE INDEX ux_downloads_destination_path ON downloads(destination_path);
        """;
    private const string RecoveryMetadataMigration = """
        ALTER TABLE downloads ADD COLUMN temporary_path TEXT COLLATE NOCASE NULL;
        ALTER TABLE downloads ADD COLUMN final_url TEXT NULL;
        ALTER TABLE downloads ADD COLUMN total_size INTEGER NULL CHECK (total_size >= 0);
        ALTER TABLE downloads ADD COLUMN etag TEXT NULL;
        ALTER TABLE downloads ADD COLUMN last_modified TEXT NULL;
        ALTER TABLE downloads ADD COLUMN supports_byte_ranges INTEGER NULL
            CHECK (supports_byte_ranges IN (0, 1));
        CREATE UNIQUE INDEX ux_downloads_temporary_path
            ON downloads(temporary_path) WHERE temporary_path IS NOT NULL;
        """;
    private const string VerifiedSha256Migration = """
        ALTER TABLE downloads ADD COLUMN verified_sha256 TEXT NULL
            CHECK (verified_sha256 IS NULL OR
                   (length(verified_sha256) = 64 AND
                    verified_sha256 NOT GLOB '*[^0-9A-F]*'));
        """;

    private static readonly (int Version, string Sql)[] Migrations =
    [
        (1, InitialMigration),
        (2, RecoveryMetadataMigration),
        (3, VerifiedSha256Migration),
    ];

    private readonly string _connectionString;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _writerLock = new(1, 1);
    private bool _initialized;

    public SqliteDownloadRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath))
        {
            throw new ArgumentException("The database path must be absolute.", nameof(databasePath));
        }

        var parent = Path.GetDirectoryName(databasePath)
            ?? throw new ArgumentException("The database path has no parent directory.", nameof(databasePath));
        Directory.CreateDirectory(parent);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString();
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, """
                CREATE TABLE IF NOT EXISTS schema_migrations (
                    version INTEGER PRIMARY KEY NOT NULL,
                    checksum TEXT NOT NULL,
                    applied_at TEXT NOT NULL
                );
                """, cancellationToken).ConfigureAwait(false);

            await using (var futureVersion = connection.CreateCommand())
            {
                futureVersion.CommandText = "SELECT MAX(version) FROM schema_migrations;";
                var maximumVersion = await futureVersion.ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (maximumVersion is long version && version > CurrentSchemaVersion)
                {
                    throw new InvalidDataException($"The database schema version {version} is newer than this application.");
                }
            }

            foreach (var migrationDefinition in Migrations)
            {
                await ApplyOrVerifyMigrationAsync(connection, migrationDefinition, cancellationToken)
                    .ConfigureAwait(false);
            }

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async ValueTask<DownloadTask?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT original_url, destination_path, state, confirmed_bytes,
                   temporary_path, final_url, total_size, etag, last_modified, supports_byte_ranges,
                   verified_sha256
            FROM downloads
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var stateValue = reader.GetInt32(2);
        if (!Enum.IsDefined(typeof(DownloadState), stateValue))
        {
            throw new InvalidDataException($"Unknown persisted download state: {stateValue}.");
        }

        var temporaryPath = reader.IsDBNull(4) ? null : reader.GetString(4);
        var remoteIdentity = ReadRemoteIdentity(reader);
        if ((temporaryPath is null) != (remoteIdentity is null))
        {
            throw new InvalidDataException("The persisted recovery metadata is incomplete.");
        }

        return DownloadTask.Restore(
            id,
            new Uri(reader.GetString(0), UriKind.Absolute),
            reader.GetString(1),
            (DownloadState)stateValue,
            reader.GetInt64(3),
            temporaryPath,
            remoteIdentity,
            reader.IsDBNull(10) ? null : reader.GetString(10));
    }

    public async ValueTask SaveAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _writerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO downloads(
                    id, original_url, destination_path, state, confirmed_bytes, created_at, updated_at,
                    temporary_path, final_url, total_size, etag, last_modified, supports_byte_ranges,
                    verified_sha256)
                VALUES ($id, $originalUrl, $destinationPath, $state, $confirmedBytes, $now, $now,
                        $temporaryPath, $finalUrl, $totalSize, $etag, $lastModified, $supportsByteRanges,
                        $verifiedSha256)
                ON CONFLICT(id) DO UPDATE SET
                    original_url = excluded.original_url,
                    destination_path = excluded.destination_path,
                    state = excluded.state,
                    confirmed_bytes = excluded.confirmed_bytes,
                    temporary_path = excluded.temporary_path,
                    final_url = excluded.final_url,
                    total_size = excluded.total_size,
                    etag = excluded.etag,
                    last_modified = excluded.last_modified,
                    supports_byte_ranges = excluded.supports_byte_ranges,
                    verified_sha256 = excluded.verified_sha256,
                    updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("$id", task.Id.ToString("D"));
            command.Parameters.AddWithValue("$originalUrl", RedactSensitiveUriComponents(task.OriginalUri));
            command.Parameters.AddWithValue("$destinationPath", task.DestinationPath);
            command.Parameters.AddWithValue("$state", (int)task.State);
            command.Parameters.AddWithValue("$confirmedBytes", task.ConfirmedBytes);
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$temporaryPath", (object?)task.TemporaryPath ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$finalUrl",
                task.RemoteIdentity is { } identity
                    ? RedactSensitiveUriComponents(identity.FinalUri)
                    : DBNull.Value);
            command.Parameters.AddWithValue("$totalSize", (object?)task.RemoteIdentity?.Length ?? DBNull.Value);
            command.Parameters.AddWithValue("$etag", (object?)task.RemoteIdentity?.EntityTag ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$lastModified",
                task.RemoteIdentity?.LastModified is { } lastModified
                    ? lastModified.ToString("O", CultureInfo.InvariantCulture)
                    : DBNull.Value);
            command.Parameters.AddWithValue(
                "$supportsByteRanges",
                task.RemoteIdentity is { } rangeIdentity
                    ? rangeIdentity.SupportsByteRanges ? 1 : 0
                    : DBNull.Value);
            command.Parameters.AddWithValue(
                "$verifiedSha256",
                (object?)task.VerifiedSha256 ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writerLock.Release();
        }
    }

    private async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask ApplyOrVerifyMigrationAsync(
        SqliteConnection connection,
        (int Version, string Sql) migrationDefinition,
        CancellationToken cancellationToken)
    {
        var expectedChecksum = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(migrationDefinition.Sql)));
        await using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT checksum FROM schema_migrations WHERE version = $version;";
        lookup.Parameters.AddWithValue("$version", migrationDefinition.Version);
        var existing = await lookup.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (existing is not null)
        {
            if (!string.Equals(existing, expectedChecksum, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"The checksum for database migration {migrationDefinition.Version} does not match the code.");
            }

            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var migration = connection.CreateCommand();
        migration.Transaction = (SqliteTransaction)transaction;
        migration.CommandText = migrationDefinition.Sql;
        await migration.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var record = connection.CreateCommand();
        record.Transaction = (SqliteTransaction)transaction;
        record.CommandText = """
            INSERT INTO schema_migrations(version, checksum, applied_at)
            VALUES ($version, $checksum, $appliedAt);
            """;
        record.Parameters.AddWithValue("$version", migrationDefinition.Version);
        record.Parameters.AddWithValue("$checksum", expectedChecksum);
        record.Parameters.AddWithValue(
            "$appliedAt",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        await record.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static RemoteIdentity? ReadRemoteIdentity(SqliteDataReader reader)
    {
        if (reader.IsDBNull(5))
        {
            if (!reader.IsDBNull(6) || !reader.IsDBNull(7) || !reader.IsDBNull(8) || !reader.IsDBNull(9))
            {
                throw new InvalidDataException("The persisted remote identity is incomplete.");
            }

            return null;
        }

        if (reader.IsDBNull(9))
        {
            throw new InvalidDataException("The persisted range capability is missing.");
        }

        DateTimeOffset? lastModified = null;
        if (!reader.IsDBNull(8))
        {
            if (!DateTimeOffset.TryParseExact(
                    reader.GetString(8),
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                throw new InvalidDataException("The persisted Last-Modified value is invalid.");
            }

            lastModified = parsed;
        }

        var supportsByteRanges = reader.GetInt64(9);
        if (supportsByteRanges is not (0 or 1))
        {
            throw new InvalidDataException("The persisted range capability is invalid.");
        }

        return new RemoteIdentity(
            new Uri(reader.GetString(5), UriKind.Absolute),
            reader.IsDBNull(6) ? null : reader.GetInt64(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            lastModified,
            supportsByteRanges == 1);
    }

    private async ValueTask<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, """
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = FULL;
                PRAGMA busy_timeout = 5000;
                """, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async ValueTask ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string RedactSensitiveUriComponents(Uri uri)
    {
        return new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty,
        }.Uri.AbsoluteUri;
    }

    public ValueTask DisposeAsync()
    {
        _initializationLock.Dispose();
        _writerLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
