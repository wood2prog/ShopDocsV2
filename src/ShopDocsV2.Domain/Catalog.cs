namespace ShopDocsV2.Domain;

/// <summary>The catalog lists that are just a name per row. Finishes and countertop colors carry extra columns, so they have their own types.</summary>
public enum CatalogList { Materials, Pulls, HardwareColors }

/// <summary>One row of a <see cref="CatalogList"/>.</summary>
public sealed class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CatalogFinish
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? HexColor { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// MaterialName matches questions.json's static countertops.material options (Quartz/Granite/...),
/// which is a different concept from the Materials catalog list (wood species used by cabinet_finishes.wood).
/// </summary>
public sealed class CatalogCountertopColor
{
    public int Id { get; set; }
    public string MaterialName { get; set; } = "";
    public string ColorName { get; set; } = "";
    public int SortOrder { get; set; }
}
