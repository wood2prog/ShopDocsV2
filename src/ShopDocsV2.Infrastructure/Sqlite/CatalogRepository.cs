using Dapper;
using Microsoft.Data.Sqlite;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Infrastructure.Sqlite;

public sealed class CatalogRepository(SqliteConnectionFactory connectionFactory) : ICatalogRepository
{
    public async Task<List<CatalogItem>> GetItemsAsync(CatalogList list)
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<NamedRow>($"SELECT id, name, sort_order FROM {TableFor(list)} ORDER BY sort_order, name");
        return rows.Select(r => new CatalogItem { Id = r.id, Name = r.name, SortOrder = r.sort_order }).ToList();
    }

    public async Task<int> AddItemAsync(CatalogList list, string name)
    {
        var table = TableFor(list);
        using var connection = connectionFactory.Create();
        var sortOrder = await NextSortOrderAsync(connection, table);
        await connection.ExecuteAsync($"INSERT INTO {table} (name, sort_order) VALUES (@name, @sortOrder)", new { name, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    public async Task UpdateItemAsync(CatalogList list, int id, string name)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync($"UPDATE {TableFor(list)} SET name = @name WHERE id = @id", new { id, name });
    }

    public Task DeleteItemAsync(CatalogList list, int id) => DeleteByIdAsync(TableFor(list), id);

    /// <summary>Maps a CatalogList to its table. Also keeps table names out of caller-supplied strings, since they're interpolated into SQL.</summary>
    internal static string TableFor(CatalogList list) => list switch
    {
        CatalogList.Materials => "catalog_materials",
        CatalogList.Pulls => "catalog_pulls",
        CatalogList.HardwareColors => "catalog_hardware_colors",
        _ => throw new ArgumentOutOfRangeException(nameof(list), list, null)
    };

    public async Task<List<CatalogFinish>> GetFinishesAsync()
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<FinishRow>(
            "SELECT id, name, hex_color, sort_order FROM catalog_finishes ORDER BY sort_order, name");
        return rows.Select(r => new CatalogFinish { Id = r.id, Name = r.name, HexColor = r.hex_color, SortOrder = r.sort_order }).ToList();
    }

    public async Task<int> AddFinishAsync(string name, string? hexColor)
    {
        using var connection = connectionFactory.Create();
        var sortOrder = await NextSortOrderAsync(connection, "catalog_finishes");
        await connection.ExecuteAsync(
            "INSERT INTO catalog_finishes (name, hex_color, sort_order) VALUES (@name, @hexColor, @sortOrder)",
            new { name, hexColor, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    public async Task UpdateFinishAsync(int id, string name, string? hexColor)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE catalog_finishes SET name = @name, hex_color = @hexColor WHERE id = @id",
            new { id, name, hexColor });
    }

    public Task DeleteFinishAsync(int id) => DeleteByIdAsync("catalog_finishes", id);

    public async Task<List<CatalogCountertopColor>> GetCountertopColorsAsync(string? materialName = null)
    {
        using var connection = connectionFactory.Create();
        var sql = "SELECT id, material_name, color_name, sort_order FROM catalog_countertop_colors";
        if (materialName is not null)
        {
            sql += " WHERE material_name = @materialName";
        }
        sql += " ORDER BY material_name, sort_order, color_name";

        var rows = await connection.QueryAsync<CountertopColorRow>(sql, new { materialName });
        return rows.Select(r => new CatalogCountertopColor
        {
            Id = r.id,
            MaterialName = r.material_name,
            ColorName = r.color_name,
            SortOrder = r.sort_order
        }).ToList();
    }

    public async Task<int> AddCountertopColorAsync(string materialName, string colorName)
    {
        using var connection = connectionFactory.Create();
        var sortOrder = 1 + await connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(sort_order), -1) FROM catalog_countertop_colors WHERE material_name = @materialName",
            new { materialName });
        await connection.ExecuteAsync(
            "INSERT INTO catalog_countertop_colors (material_name, color_name, sort_order) VALUES (@materialName, @colorName, @sortOrder)",
            new { materialName, colorName, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    public async Task UpdateCountertopColorAsync(int id, string materialName, string colorName)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE catalog_countertop_colors SET material_name = @materialName, color_name = @colorName WHERE id = @id",
            new { id, materialName, colorName });
    }

    public Task DeleteCountertopColorAsync(int id) => DeleteByIdAsync("catalog_countertop_colors", id);

    public async Task<List<CatalogAccessory>> GetAccessoriesAsync()
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<AccessoryRow>(
            "SELECT id, name, model_number, url, sort_order FROM catalog_accessories ORDER BY sort_order, name");
        return rows.Select(r => new CatalogAccessory
        {
            Id = r.id,
            Name = r.name,
            ModelNumber = r.model_number,
            Url = r.url,
            SortOrder = r.sort_order
        }).ToList();
    }

    public async Task<int> AddAccessoryAsync(string name, string? modelNumber, string? url)
    {
        using var connection = connectionFactory.Create();
        var sortOrder = await NextSortOrderAsync(connection, "catalog_accessories");
        await connection.ExecuteAsync(
            "INSERT INTO catalog_accessories (name, model_number, url, sort_order) VALUES (@name, @modelNumber, @url, @sortOrder)",
            new { name, modelNumber, url, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    public async Task UpdateAccessoryAsync(int id, string name, string? modelNumber, string? url)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE catalog_accessories SET name = @name, model_number = @modelNumber, url = @url WHERE id = @id",
            new { id, name, modelNumber, url });
    }

    public Task DeleteAccessoryAsync(int id) => DeleteByIdAsync("catalog_accessories", id);

    private async Task DeleteByIdAsync(string table, int id)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync($"DELETE FROM {table} WHERE id = @id", new { id });
    }

    private static Task<int> NextSortOrderAsync(SqliteConnection connection, string table) =>
        connection.ExecuteScalarAsync<int>($"SELECT 1 + COALESCE(MAX(sort_order), -1) FROM {table}");

    private sealed class NamedRow
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int sort_order { get; set; }
    }

    private sealed class FinishRow
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string? hex_color { get; set; }
        public int sort_order { get; set; }
    }

    private sealed class CountertopColorRow
    {
        public int id { get; set; }
        public string material_name { get; set; } = "";
        public string color_name { get; set; } = "";
        public int sort_order { get; set; }
    }

    private sealed class AccessoryRow
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string? model_number { get; set; }
        public string? url { get; set; }
        public int sort_order { get; set; }
    }
}
