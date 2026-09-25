namespace ShopDocsV2.WinForms;

/// <summary>Free-typing ComboBox with suggestions from a catalog list, replacing the original's HTML datalist input.</summary>
internal sealed class CatalogAutocompleteComboBox : ComboBox
{
    private List<string> _options = [];

    public CatalogAutocompleteComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDown;
        ComboBoxSubstringFilter.Attach(this, () => _options);
    }

    /// <summary>Reloads the option list (e.g. after a CatalogFilterBy sibling changes), keeping preserveValue if it's still valid.</summary>
    public void RefreshOptions(IReadOnlyList<string> options, string? preserveValue = null)
    {
        _options = [.. options];
        Items.Clear();
        foreach (var option in options)
        {
            Items.Add(option);
        }

        Text = preserveValue is not null && options.Contains(preserveValue) ? preserveValue : "";
    }
}
