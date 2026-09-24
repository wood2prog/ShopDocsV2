using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface ICatalogRepository
{
    Task<List<CatalogItem>> GetItemsAsync(CatalogList list, CancellationToken ct = default);
    Task<int> AddItemAsync(CatalogList list, string name, CancellationToken ct = default);
    Task UpdateItemAsync(CatalogList list, int id, string name, CancellationToken ct = default);
    Task DeleteItemAsync(CatalogList list, int id, CancellationToken ct = default);

    Task<List<CatalogFinish>> GetFinishesAsync(CancellationToken ct = default);
    Task<int> AddFinishAsync(string name, string? hexColor, CancellationToken ct = default);
    Task UpdateFinishAsync(int id, string name, string? hexColor, CancellationToken ct = default);
    Task DeleteFinishAsync(int id, CancellationToken ct = default);

    Task<List<CatalogCountertopColor>> GetCountertopColorsAsync(string? materialName = null, CancellationToken ct = default);
    Task<int> AddCountertopColorAsync(string materialName, string colorName, CancellationToken ct = default);
    Task UpdateCountertopColorAsync(int id, string materialName, string colorName, CancellationToken ct = default);
    Task DeleteCountertopColorAsync(int id, CancellationToken ct = default);

}
