namespace ShopDocsV2.Domain;

public sealed class QuestionSet
{
    public List<QuestionSection> Sections { get; set; } = new();
}

public sealed class QuestionSection
{
    public string Title { get; set; } = "";
    public List<QuestionDef> Questions { get; set; } = new();
}

/// <summary>
/// Dimension is free text limited to measurement characters (e.g. 30 1/2); anywhere that doesn't special-case it,
/// it behaves exactly like Text, and it's stored and printed as text.
/// </summary>
public enum QuestionType { Text, Select, Checkbox, Number, Textarea, List, Dimension }

public sealed class QuestionDef
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public QuestionType Type { get; set; } = QuestionType.Text;
    public List<string>? Options { get; set; }
    public string? Default { get; set; }
    public ShowIfCondition? ShowIf { get; set; }

    /// <summary>Name of a catalog list to source dropdown options from: materials/finishes/countertops/pulls/hardware_colors/accessories.</summary>
    public string? CatalogSource { get; set; }

    /// <summary>Id of a sibling field whose value narrows CatalogSource (only countertops.color -&gt; material today).</summary>
    public string? CatalogFilterBy { get; set; }

    public bool ColorPreview { get; set; }

    /// <summary>
    /// Only used on a List's ItemFields backed by the accessories catalog: adds a read-only Product Page column
    /// after the item's fields, showing the picked accessory as a link that opens its web page.
    /// </summary>
    public bool CatalogLink { get; set; }
    public string? Placeholder { get; set; }

    /// <summary>Only used when Type == List.</summary>
    public string? AddLabel { get; set; }

    /// <summary>Only used when Type == List.</summary>
    public List<QuestionDef>? ItemFields { get; set; }

    /// <summary>
    /// Only used on a List's ItemFields: in print/text output, adjacent fields sharing a PrintGroup are
    /// combined into one part, e.g. "Cutout - 30W x 36H x 24D", skipping blank ones.
    /// </summary>
    public string? PrintGroup { get; set; }

    /// <summary>Appended to this field's value inside its PrintGroup, e.g. "W" in "30W".</summary>
    public string? PrintSuffix { get; set; }

    /// <summary>
    /// Only used when Type == List: the default key for jumping to this list and starting a new line, e.g. "Ctrl+F".
    /// The user can rebind it under Tools &gt; Keyboard Shortcuts.
    /// </summary>
    public string? Shortcut { get; set; }
}

public sealed class ShowIfCondition
{
    public string Field { get; set; } = "";

    /// <summary>The value Field must equal for the dependent question to be visible. Named EqualsValue, not Equals, to avoid shadowing object.Equals.</summary>
    public string EqualsValue { get; set; } = "";
}
