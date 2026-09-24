using Microsoft.Data.Sqlite;
using ShopDocsV2.Infrastructure.Files;

namespace ShopDocsV2.Infrastructure.Sqlite;

public sealed class SqliteConnectionFactory
{
    private readonly string _dbPath;

    public SqliteConnectionFactory(string? dbPath = null)
    {
        _dbPath = dbPath ?? GetDefaultDbPath();
    }

    public static string GetDefaultDbPath() => Path.Combine(AppDataPaths.DataDirectory, "shopdocs.db");

    public SqliteConnection Create()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var pragmaCommand = connection.CreateCommand();
        // Rollback journal (not WAL) keeps the database a single file at rest, so it's safe to back up or
        // sync from Documents. WAL's concurrency gains don't matter for one user in one process. Opening a
        // pre-1.0.12 WAL database with this checkpoints it and removes its -wal/-shm files.
        pragmaCommand.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA foreign_keys=ON;";
        pragmaCommand.ExecuteNonQuery();

        return connection;
    }
}
