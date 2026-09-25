using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>Accessories CRUD grid: name, model number and a pasted web link, with an "Open" link per row that opens it in the browser.</summary>
public partial class AccessoriesGrid : UserControl
{
    private const string NameColumnName = "Name";
    private const string ModelColumnName = "Model";
    private const string UrlColumnName = "Url";
    private const string OpenColumnName = "Open";

    private ICatalogRepository? _catalogRepository;

    /// <summary>The rows, for the Catalog Manager's search.</summary>
    internal DataGridView Grid => grid;

    public AccessoriesGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        grid.KeyDown += Grid_KeyDown;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var accessories = await _catalogRepository!.GetAccessoriesAsync();
        grid.Rows.Clear();
        foreach (var accessory in accessories)
        {
            var rowIndex = grid.Rows.Add(accessory.Name, accessory.ModelNumber ?? "", accessory.Url ?? "");
            grid.Rows[rowIndex].Tag = accessory.Id;
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        var name = PromptDialog.ShowInput(FindForm(), "Add Accessory", "Name:", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var id = await _catalogRepository!.AddAccessoryAsync(name.Trim(), null, null);
        await ReloadAsync();

        // Straight to the model number, the next thing to fill in.
        if (grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is int rowId && rowId == id) is { } row)
        {
            CatalogGrid.ShowRow(grid, row.Index, ModelColumnName);
        }
    }

    private async void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        await CommitRowAsync(grid.Rows[e.RowIndex], id);
    }

    private async Task CommitRowAsync(DataGridViewRow row, int id)
    {
        var name = CellText(row, NameColumnName);
        if (name.Length == 0)
        {
            return;
        }

        var modelNumber = CellText(row, ModelColumnName);
        var url = CellText(row, UrlColumnName);
        await _catalogRepository!.UpdateAccessoryAsync(id, name,
            modelNumber.Length == 0 ? null : modelNumber, url.Length == 0 ? null : url);
    }

    private static string CellText(DataGridViewRow row, string columnName) =>
        (row.Cells[columnName].Value as string ?? "").Trim();

    /// <summary>The grid doesn't paste on its own; Ctrl+V on a selected (not yet editing) text cell replaces its value.</summary>
    private async void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyData != (Keys.Control | Keys.V) || grid.CurrentCell is not DataGridViewTextBoxCell cell ||
            cell.ReadOnly || grid.Rows[cell.RowIndex].Tag is not int id || !Clipboard.ContainsText())
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        cell.Value = Clipboard.GetText().Trim();
        await CommitRowAsync(grid.Rows[cell.RowIndex], id);
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        var row = grid.Rows[e.RowIndex];
        switch (grid.Columns[e.ColumnIndex].Name)
        {
            case CatalogGrid.RemoveColumnName:
                if (CatalogGrid.ConfirmDelete(this, CellText(row, NameColumnName)))
                {
                    await _catalogRepository!.DeleteAccessoryAsync(id);
                    await ReloadAsync();
                }
                break;
            case OpenColumnName:
                WebLink.Open(FindForm(), CellText(row, UrlColumnName));
                break;
        }
    }
}
