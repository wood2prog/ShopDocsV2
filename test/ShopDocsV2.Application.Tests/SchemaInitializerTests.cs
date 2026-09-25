using Dapper;
using Microsoft.Data.Sqlite;
using ShopDocsV2.Infrastructure.Sqlite;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class SchemaInitializerTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-test-{Guid.NewGuid():N}.db");

    [Fact]
    public void Initialize_CreatesAllTablesAndSeedsCatalogRows()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        new SchemaInitializer(factory).Initialize();

        using var connection = factory.Create();

        var tableNames = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name").ToList();

        Assert.Equal(
            new[]
            {
                "catalog_accessories", "catalog_countertop_colors", "catalog_finishes", "catalog_hardware_colors",
                "catalog_materials", "catalog_pulls", "jobs", "room_answers",
                "room_list_item_values", "room_list_items", "rooms", "schema_version"
            },
            tableNames);

        Assert.Equal(16, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_materials"));
        Assert.Equal(263, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_finishes"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_pulls"));
        Assert.Equal(6, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_hardware_colors"));
        Assert.Equal(40 + 14, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_countertop_colors"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM schema_version"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT version FROM schema_version"));
        Assert.All(
            connection.Query<string?>("SELECT hex_color FROM catalog_finishes"),
            hex => Assert.Null(hex));
    }

    [Fact]
    public void Initialize_CalledTwice_DoesNotDuplicateSeedRows()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        var initializer = new SchemaInitializer(factory);

        initializer.Initialize();
        initializer.Initialize();

        using var connection = factory.Create();
        Assert.Equal(16, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM catalog_materials"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM schema_version"));
    }

    [Fact]
    public void Initialize_OlderDatabase_DropsRemovedHingesAndGuidesTables()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        using (var connection = factory.Create())
        {
            connection.Execute("CREATE TABLE catalog_hinges (id INTEGER PRIMARY KEY, name TEXT, sort_order INTEGER)");
            connection.Execute("CREATE TABLE catalog_guides (id INTEGER PRIMARY KEY, name TEXT, sort_order INTEGER)");
        }

        new SchemaInitializer(factory).Initialize();

        using var check = factory.Create();
        Assert.Equal(0, check.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('catalog_hinges', 'catalog_guides')"));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
