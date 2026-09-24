using Microsoft.Data.Sqlite;
using ShopDocsV2.Domain;
using ShopDocsV2.Infrastructure.Sqlite;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class CatalogRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-catalog-test-{Guid.NewGuid():N}.db");
    private readonly CatalogRepository _repository;

    public CatalogRepositoryTests()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        new SchemaInitializer(factory).Initialize();
        _repository = new CatalogRepository(factory);
    }

    [Fact]
    public async Task GetItemsAsync_Materials_ReturnsSeededRowsFromCatalogJson()
    {
        var materials = await _repository.GetItemsAsync(CatalogList.Materials);

        Assert.Equal(16, materials.Count);
        Assert.Contains(materials, m => m.Name == "Maple");
    }

    [Fact]
    public async Task AddUpdateDeleteItem_RoundTrips()
    {
        var id = await _repository.AddItemAsync(CatalogList.Materials, "Bamboo");
        Assert.Contains((await _repository.GetItemsAsync(CatalogList.Materials)), m => m.Id == id && m.Name == "Bamboo");

        await _repository.UpdateItemAsync(CatalogList.Materials, id, "Bamboo (Renamed)");
        Assert.Contains((await _repository.GetItemsAsync(CatalogList.Materials)), m => m.Id == id && m.Name == "Bamboo (Renamed)");

        await _repository.DeleteItemAsync(CatalogList.Materials, id);
        Assert.DoesNotContain((await _repository.GetItemsAsync(CatalogList.Materials)), m => m.Id == id);
    }

    [Fact]
    public async Task AddUpdateDeleteFinish_RoundTripsHexColor()
    {
        var id = await _repository.AddFinishAsync("Custom Blue", null);
        var afterAdd = (await _repository.GetFinishesAsync()).Single(f => f.Id == id);
        Assert.Equal("Custom Blue", afterAdd.Name);
        Assert.Null(afterAdd.HexColor);

        await _repository.UpdateFinishAsync(id, "Custom Blue", "#3366CC");
        var afterUpdate = (await _repository.GetFinishesAsync()).Single(f => f.Id == id);
        Assert.Equal("#3366CC", afterUpdate.HexColor);

        await _repository.DeleteFinishAsync(id);
        Assert.DoesNotContain((await _repository.GetFinishesAsync()), f => f.Id == id);
    }

    [Fact]
    public async Task GetCountertopColorsAsync_FiltersByMaterialName()
    {
        var quartzId = await _repository.AddCountertopColorAsync("Quartz", "Test Quartz Color");
        var graniteId = await _repository.AddCountertopColorAsync("Granite", "Test Granite Color");

        var quartzColors = await _repository.GetCountertopColorsAsync("Quartz");
        Assert.Contains(quartzColors, c => c.Id == quartzId);
        Assert.DoesNotContain(quartzColors, c => c.Id == graniteId);

        var all = await _repository.GetCountertopColorsAsync();
        Assert.Contains(all, c => c.Id == quartzId);
        Assert.Contains(all, c => c.Id == graniteId);

        await _repository.UpdateCountertopColorAsync(quartzId, "Quartz", "Renamed Quartz Color");
        Assert.Contains((await _repository.GetCountertopColorsAsync("Quartz")), c => c.Id == quartzId && c.ColorName == "Renamed Quartz Color");

        await _repository.DeleteCountertopColorAsync(quartzId);
        await _repository.DeleteCountertopColorAsync(graniteId);
        Assert.DoesNotContain((await _repository.GetCountertopColorsAsync()), c => c.Id == quartzId || c.Id == graniteId);
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
