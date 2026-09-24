namespace ShopDocsV2.WinForms;

/// <summary>Pieces shared by the Catalog Manager's grids (SimpleCatalogGrid, FinishesGrid, CountertopColorsGrid).</summary>
internal static class CatalogGrid
{
    public const string RemoveColumnName = "Remove";

    /// <summary>The narrow ✕ button column at the end of each row.</summary>
    public static DataGridViewButtonColumn CreateRemoveColumn() => new()
    {
        Name = RemoveColumnName,
        HeaderText = "",
        Text = "✕",
        UseColumnTextForButtonValue = true,
        Width = 32,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.None
    };

    public static bool IsRemoveColumn(DataGridView grid, int columnIndex) =>
        columnIndex >= 0 && grid.Columns[columnIndex].Name == RemoveColumnName;

    public static bool ConfirmDelete(Control owner, string name) =>
        MessageBox.Show(owner.FindForm(), $"Delete \"{name}\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
}
