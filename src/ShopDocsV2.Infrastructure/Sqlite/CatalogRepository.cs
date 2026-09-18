using Dapper;
using Microsoft.Data.Sqlite;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Infrastructure.Sqlite;

public sealed class CatalogRepository(SqliteConnectionFactory connectionFactory) : ICatalogRepository
{
    public Task<List<CatalogMaterial>> GetMaterialsAsync(CancellationToken ct = default) =>
        GetNamedAsync("catalog_materials", r => new CatalogMaterial { Id = r.id, Name = r.name, SortOrder = r.sort_order });
    public Task<int> AddMaterialAsync(string name, CancellationToken ct = default) => AddNamedAsync("catalog_materials", name);
    public Task UpdateMaterialAsync(int id, string name, CancellationToken ct = default) => UpdateNamedAsync("catalog_materials", id, name);
    public Task DeleteMaterialAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_materials", id);

    public Task<List<CatalogPull>> GetPullsAsync(CancellationToken ct = default) =>
        GetNamedAsync("catalog_pulls", r => new CatalogPull { Id = r.id, Name = r.name, SortOrder = r.sort_order });
    public Task<int> AddPullAsync(string name, CancellationToken ct = default) => AddNamedAsync("catalog_pulls", name);
    public Task UpdatePullAsync(int id, string name, CancellationToken ct = default) => UpdateNamedAsync("catalog_pulls", id, name);
    public Task DeletePullAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_pulls", id);

    public Task<List<CatalogHardwareColor>> GetHardwareColorsAsync(CancellationToken ct = default) =>
        GetNamedAsync("catalog_hardware_colors", r => new CatalogHardwareColor { Id = r.id, Name = r.name, SortOrder = r.sort_order });
    public Task<int> AddHardwareColorAsync(string name, CancellationToken ct = default) => AddNamedAsync("catalog_hardware_colors", name);
    public Task UpdateHardwareColorAsync(int id, string name, CancellationToken ct = default) => UpdateNamedAsync("catalog_hardware_colors", id, name);
    public Task DeleteHardwareColorAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_hardware_colors", id);

    public Task<List<CatalogHinge>> GetHingesAsync(CancellationToken ct = default) =>
        GetNamedAsync("catalog_hinges", r => new CatalogHinge { Id = r.id, Name = r.name, SortOrder = r.sort_order });
    public Task<int> AddHingeAsync(string name, CancellationToken ct = default) => AddNamedAsync("catalog_hinges", name);
    public Task UpdateHingeAsync(int id, string name, CancellationToken ct = default) => UpdateNamedAsync("catalog_hinges", id, name);
    public Task DeleteHingeAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_hinges", id);

    public Task<List<CatalogGuide>> GetGuidesAsync(CancellationToken ct = default) =>
        GetNamedAsync("catalog_guides", r => new CatalogGuide { Id = r.id, Name = r.name, SortOrder = r.sort_order });
    public Task<int> AddGuideAsync(string name, CancellationToken ct = default) => AddNamedAsync("catalog_guides", name);
    public Task UpdateGuideAsync(int id, string name, CancellationToken ct = default) => UpdateNamedAsync("catalog_guides", id, name);
    public Task DeleteGuideAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_guides", id);

    public async Task<List<CatalogFinish>> GetFinishesAsync(CancellationToken ct = default)
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<FinishRow>(
            "SELECT id, name, hex_color, sort_order FROM catalog_finishes ORDER BY sort_order, name");
        return rows.Select(r => new CatalogFinish { Id = r.id, Name = r.name, HexColor = r.hex_color, SortOrder = r.sort_order }).ToList();
    }

    public async Task<int> AddFinishAsync(string name, string? hexColor, CancellationToken ct = default)
    {
        using var connection = connectionFactory.Create();
        var sortOrder = await NextSortOrderAsync(connection, "catalog_finishes");
        await connection.ExecuteAsync(
            "INSERT INTO catalog_finishes (name, hex_color, sort_order) VALUES (@name, @hexColor, @sortOrder)",
            new { name, hexColor, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    public async Task UpdateFinishAsync(int id, string name, string? hexColor, CancellationToken ct = default)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE catalog_finishes SET name = @name, hex_color = @hexColor WHERE id = @id",
            new { id, name, hexColor });
    }

    public Task DeleteFinishAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_finishes", id);

    public async Task<List<CatalogCountertopColor>> GetCountertopColorsAsync(string? materialName = null, CancellationToken ct = default)
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

    public async Task<int> AddCountertopColorAsync(string materialName, string colorName, CancellationToken ct = default)
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

    public async Task UpdateCountertopColorAsync(int id, string materialName, string colorName, CancellationToken ct = default)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(
            "UPDATE catalog_countertop_colors SET material_name = @materialName, color_name = @colorName WHERE id = @id",
            new { id, materialName, colorName });
    }

    public Task DeleteCountertopColorAsync(int id, CancellationToken ct = default) => DeleteByIdAsync("catalog_countertop_colors", id);

    private async Task<List<T>> GetNamedAsync<T>(string table, Func<NamedRow, T> map)
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<NamedRow>($"SELECT id, name, sort_order FROM {table} ORDER BY sort_order, name");
        return rows.Select(map).ToList();
    }

    private async Task<int> AddNamedAsync(string table, string name)
    {
        using var connection = connectionFactory.Create();
        var sortOrder = await NextSortOrderAsync(connection, table);
        await connection.ExecuteAsync($"INSERT INTO {table} (name, sort_order) VALUES (@name, @sortOrder)", new { name, sortOrder });
        return await connection.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
    }

    private async Task UpdateNamedAsync(string table, int id, string name)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync($"UPDATE {table} SET name = @name WHERE id = @id", new { id, name });
    }

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
}
