using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Data.Sqlite;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Infrastructure.Sqlite;

public sealed class SchemaInitializer(SqliteConnectionFactory connectionFactory)
{
    private const string SchemaResourceName = "ShopDocsV2.Infrastructure.Sqlite.Schema.sql";
    private const string CatalogSeedResourceName = "ShopDocsV2.Infrastructure.Sqlite.SeedData.catalog.seed.json";

    public void Initialize()
    {
        using var connection = connectionFactory.Create();

        using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = ReadEmbeddedResourceText(SchemaResourceName);
            schemaCommand.ExecuteNonQuery();
        }

        if (connection.ExecuteScalar<int?>("SELECT version FROM schema_version LIMIT 1") is null)
        {
            SeedCatalog(connection, ReadEmbeddedResourceText(CatalogSeedResourceName));
            connection.Execute("INSERT INTO schema_version (version) VALUES (1)");
        }
    }

    private static void SeedCatalog(SqliteConnection connection, string catalogSeedJson)
    {
        var seed = JsonSerializer.Deserialize<CatalogSeedDto>(catalogSeedJson)
            ?? throw new InvalidOperationException("Catalog seed resource is empty or malformed.");

        using var transaction = connection.BeginTransaction();

        InsertNamed(connection, transaction, CatalogRepository.TableFor(CatalogList.Materials), seed.Materials);
        InsertNamed(connection, transaction, "catalog_finishes", seed.Finishes);
        InsertNamed(connection, transaction, CatalogRepository.TableFor(CatalogList.Pulls), seed.Pulls);
        InsertNamed(connection, transaction, CatalogRepository.TableFor(CatalogList.HardwareColors), seed.HardwareColors);

        var sortOrder = 0;
        foreach (var (materialName, colorNames) in seed.Countertops)
        {
            foreach (var colorName in colorNames)
            {
                connection.Execute(
                    "INSERT INTO catalog_countertop_colors (material_name, color_name, sort_order) VALUES (@materialName, @colorName, @sortOrder)",
                    new { materialName, colorName, sortOrder = sortOrder++ },
                    transaction);
            }
        }

        transaction.Commit();
    }

    private static void InsertNamed(SqliteConnection connection, SqliteTransaction transaction, string table, List<string> names)
    {
        for (var i = 0; i < names.Count; i++)
        {
            connection.Execute(
                $"INSERT INTO {table} (name, sort_order) VALUES (@name, @sortOrder)",
                new { name = names[i], sortOrder = i },
                transaction);
        }
    }

    private static string ReadEmbeddedResourceText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class CatalogSeedDto
    {
        [JsonPropertyName("materials")]
        public List<string> Materials { get; set; } = new();

        [JsonPropertyName("finishes")]
        public List<string> Finishes { get; set; } = new();

        [JsonPropertyName("countertops")]
        public Dictionary<string, List<string>> Countertops { get; set; } = new();

        [JsonPropertyName("pulls")]
        public List<string> Pulls { get; set; } = new();

        [JsonPropertyName("hardware_colors")]
        public List<string> HardwareColors { get; set; } = new();
    }
}
