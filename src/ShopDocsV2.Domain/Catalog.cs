namespace ShopDocsV2.Domain;

public sealed class CatalogMaterial
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
/// which is a different concept from CatalogMaterial (wood species used by cabinet_finishes.wood).
/// </summary>
public sealed class CatalogCountertopColor
{
    public int Id { get; set; }
    public string MaterialName { get; set; } = "";
    public string ColorName { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CatalogPull
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CatalogHardwareColor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CatalogHinge
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CatalogGuide
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}
