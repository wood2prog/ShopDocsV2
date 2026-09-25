using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>Reusable add/rename/delete grid for the five catalog kinds that are just an id/name/sort_order table.</summary>
public partial class SimpleCatalogGrid : UserControl
{
    private ICatalogRepository? _catalogRepository;
    private CatalogList _list;

    /// <summary>The rows, for the Catalog Manager's search.</summary>
    internal DataGridView Grid => grid;

    public SimpleCatalogGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(ICatalogRepository catalogRepository, CatalogList list)
    {
        _catalogRepository = catalogRepository;
        _list = list;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var items = await _catalogRepository!.GetItemsAsync(_list);
        grid.Rows.Clear();
        foreach (var item in items)
        {
            var rowIndex = grid.Rows.Add(item.Name);
            grid.Rows[rowIndex].Tag = item.Id;
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        var name = PromptDialog.ShowInput(FindForm(), "Add", "Name:", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _catalogRepository!.AddItemAsync(_list, name.Trim());
        await ReloadAsync();
    }

    private async void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        var name = grid.Rows[e.RowIndex].Cells[0].Value as string ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _catalogRepository!.UpdateItemAsync(_list, id, name.Trim());
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || !CatalogGrid.IsRemoveColumn(grid, e.ColumnIndex) || grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        if (!CatalogGrid.ConfirmDelete(this, grid.Rows[e.RowIndex].Cells[0].Value as string ?? ""))
        {
            return;
        }

        await _catalogRepository!.DeleteItemAsync(_list, id);
        await ReloadAsync();
    }
}
