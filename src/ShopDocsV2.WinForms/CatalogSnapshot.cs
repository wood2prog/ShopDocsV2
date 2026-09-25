using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>
/// A one-time load of all catalog tables, held in memory for the session (mirrors the original
/// app's single global CATALOG object) so building room UI never needs to await a query mid-render.
/// </summary>
internal sealed class CatalogSnapshot
{
    public IReadOnlyDictionary<CatalogList, List<CatalogItem>> Lists { get; init; } = new Dictionary<CatalogList, List<CatalogItem>>();
    public List<CatalogFinish> Finishes { get; init; } = [];
    public List<CatalogCountertopColor> CountertopColors { get; init; } = [];

    public static async Task<CatalogSnapshot> LoadAsync(ICatalogRepository repository)
    {
        var lists = new Dictionary<CatalogList, List<CatalogItem>>();
        foreach (var list in Enum.GetValues<CatalogList>())
        {
            lists[list] = await repository.GetItemsAsync(list);
        }

        return new CatalogSnapshot
        {
            Lists = lists,
            Finishes = await repository.GetFinishesAsync(),
            CountertopColors = await repository.GetCountertopColorsAsync()
        };
    }

    /// <summary>Mirrors the original app's resolveCatalogValues + sortCatalogValues.</summary>
    public IReadOnlyList<string> Resolve(string? catalogSource, string? filterValue) => catalogSource switch
    {
        "finishes" => SortedNames(Finishes.Select(f => f.Name)),
        "countertops" => string.IsNullOrEmpty(filterValue)
            ? []
            : SortedNames(CountertopColors.Where(c => c.MaterialName == filterValue).Select(c => c.ColorName)),
        _ => ListForSource(catalogSource) is { } list && Lists.TryGetValue(list, out var items)
            ? SortedNames(items.Select(i => i.Name))
            : []
    };

    /// <summary>Maps a questions.json catalogSource name to its CatalogList, or null if it isn't one of the name-only lists.</summary>
    private static CatalogList? ListForSource(string? catalogSource) => catalogSource switch
    {
        "materials" => CatalogList.Materials,
        "pulls" => CatalogList.Pulls,
        "hardware_colors" => CatalogList.HardwareColors,
        _ => null
    };

    public string? HexFor(string finishName) => Finishes.FirstOrDefault(f => f.Name == finishName)?.HexColor;

    private static List<string> SortedNames(IEnumerable<string> names) =>
        names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
}
