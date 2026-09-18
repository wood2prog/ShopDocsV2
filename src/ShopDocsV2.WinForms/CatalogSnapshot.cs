using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>
/// A one-time load of all catalog tables, held in memory for the session (mirrors the original
/// app's single global CATALOG object) so building room UI never needs to await a query mid-render.
/// </summary>
internal sealed class CatalogSnapshot
{
    public List<CatalogMaterial> Materials { get; init; } = [];
    public List<CatalogFinish> Finishes { get; init; } = [];
    public List<CatalogCountertopColor> CountertopColors { get; init; } = [];
    public List<CatalogPull> Pulls { get; init; } = [];
    public List<CatalogHardwareColor> HardwareColors { get; init; } = [];
    public List<CatalogHinge> Hinges { get; init; } = [];
    public List<CatalogGuide> Guides { get; init; } = [];

    public static async Task<CatalogSnapshot> LoadAsync(ICatalogRepository repository, CancellationToken ct = default) => new()
    {
        Materials = await repository.GetMaterialsAsync(ct),
        Finishes = await repository.GetFinishesAsync(ct),
        CountertopColors = await repository.GetCountertopColorsAsync(ct: ct),
        Pulls = await repository.GetPullsAsync(ct),
        HardwareColors = await repository.GetHardwareColorsAsync(ct),
        Hinges = await repository.GetHingesAsync(ct),
        Guides = await repository.GetGuidesAsync(ct)
    };

    /// <summary>Mirrors the original app's resolveCatalogValues + sortCatalogValues.</summary>
    public IReadOnlyList<string> Resolve(string? catalogSource, string? filterValue) => catalogSource switch
    {
        "materials" => SortedNames(Materials.Select(m => m.Name)),
        "finishes" => SortedNames(Finishes.Select(f => f.Name)),
        "countertops" => string.IsNullOrEmpty(filterValue)
            ? []
            : SortedNames(CountertopColors.Where(c => c.MaterialName == filterValue).Select(c => c.ColorName)),
        "pulls" => SortedNames(Pulls.Select(p => p.Name)),
        "hardware_colors" => SortedNames(HardwareColors.Select(h => h.Name)),
        "hinges" => SortedNames(Hinges.Select(h => h.Name)),
        "guides" => SortedNames(Guides.Select(g => g.Name)),
        _ => []
    };

    public string? HexFor(string finishName) => Finishes.FirstOrDefault(f => f.Name == finishName)?.HexColor;

    private static List<string> SortedNames(IEnumerable<string> names) =>
        names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
}
