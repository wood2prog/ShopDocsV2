using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface ICatalogRepository
{
    Task<List<CatalogItem>> GetItemsAsync(CatalogList list);
    Task<int> AddItemAsync(CatalogList list, string name);
    Task UpdateItemAsync(CatalogList list, int id, string name);
    Task DeleteItemAsync(CatalogList list, int id);

    Task<List<CatalogFinish>> GetFinishesAsync();
    Task<int> AddFinishAsync(string name, string? hexColor);
    Task UpdateFinishAsync(int id, string name, string? hexColor);
    Task DeleteFinishAsync(int id);

    Task<List<CatalogCountertopColor>> GetCountertopColorsAsync(string? materialName = null);
    Task<int> AddCountertopColorAsync(string materialName, string colorName);
    Task UpdateCountertopColorAsync(int id, string materialName, string colorName);
    Task DeleteCountertopColorAsync(int id);

    Task<List<CatalogAccessory>> GetAccessoriesAsync();
    Task<int> AddAccessoryAsync(string name, string? modelNumber, string? url);
    Task UpdateAccessoryAsync(int id, string name, string? modelNumber, string? url);
    Task DeleteAccessoryAsync(int id);

}
