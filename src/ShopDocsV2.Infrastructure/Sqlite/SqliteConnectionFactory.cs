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
        pragmaCommand.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragmaCommand.ExecuteNonQuery();

        return connection;
    }
}
