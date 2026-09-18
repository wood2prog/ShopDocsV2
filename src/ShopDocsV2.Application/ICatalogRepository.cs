using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface ICatalogRepository
{
    Task<List<CatalogMaterial>> GetMaterialsAsync(CancellationToken ct = default);
    Task<int> AddMaterialAsync(string name, CancellationToken ct = default);
    Task UpdateMaterialAsync(int id, string name, CancellationToken ct = default);
    Task DeleteMaterialAsync(int id, CancellationToken ct = default);

    Task<List<CatalogFinish>> GetFinishesAsync(CancellationToken ct = default);
    Task<int> AddFinishAsync(string name, string? hexColor, CancellationToken ct = default);
    Task UpdateFinishAsync(int id, string name, string? hexColor, CancellationToken ct = default);
    Task DeleteFinishAsync(int id, CancellationToken ct = default);

    Task<List<CatalogCountertopColor>> GetCountertopColorsAsync(string? materialName = null, CancellationToken ct = default);
    Task<int> AddCountertopColorAsync(string materialName, string colorName, CancellationToken ct = default);
    Task UpdateCountertopColorAsync(int id, string materialName, string colorName, CancellationToken ct = default);
    Task DeleteCountertopColorAsync(int id, CancellationToken ct = default);

    Task<List<CatalogPull>> GetPullsAsync(CancellationToken ct = default);
    Task<int> AddPullAsync(string name, CancellationToken ct = default);
    Task UpdatePullAsync(int id, string name, CancellationToken ct = default);
    Task DeletePullAsync(int id, CancellationToken ct = default);

    Task<List<CatalogHardwareColor>> GetHardwareColorsAsync(CancellationToken ct = default);
    Task<int> AddHardwareColorAsync(string name, CancellationToken ct = default);
    Task UpdateHardwareColorAsync(int id, string name, CancellationToken ct = default);
    Task DeleteHardwareColorAsync(int id, CancellationToken ct = default);

    Task<List<CatalogHinge>> GetHingesAsync(CancellationToken ct = default);
    Task<int> AddHingeAsync(string name, CancellationToken ct = default);
    Task UpdateHingeAsync(int id, string name, CancellationToken ct = default);
    Task DeleteHingeAsync(int id, CancellationToken ct = default);

    Task<List<CatalogGuide>> GetGuidesAsync(CancellationToken ct = default);
    Task<int> AddGuideAsync(string name, CancellationToken ct = default);
    Task UpdateGuideAsync(int id, string name, CancellationToken ct = default);
    Task DeleteGuideAsync(int id, CancellationToken ct = default);
}
