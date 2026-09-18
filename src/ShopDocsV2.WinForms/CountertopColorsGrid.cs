using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>Countertop Colors CRUD grid: one row per (material, color), material picked from the same static list questions.json uses.</summary>
public partial class CountertopColorsGrid : UserControl
{
    internal static readonly string[] MaterialOptions =
        ["Laminate", "Granite", "Quartz", "Butcher Block", "Solid Surface", "Tile", "Other"];

    private const int MaterialColumnIndex = 0;
    private const int ColorColumnIndex = 1;

    private ICatalogRepository? _catalogRepository;

    public CountertopColorsGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var colors = await _catalogRepository!.GetCountertopColorsAsync();
        grid.Rows.Clear();
        foreach (var color in colors)
        {
            var rowIndex = grid.Rows.Add(color.MaterialName, color.ColorName);
            grid.Rows[rowIndex].Tag = color.Id;
        }
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        var rowIndex = grid.Rows.Add(MaterialOptions[0], "");
        grid.CurrentCell = grid.Rows[rowIndex].Cells[ColorColumnIndex];
        grid.BeginEdit(true);
    }

    private async void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        var row = grid.Rows[e.RowIndex];
        var material = row.Cells[MaterialColumnIndex].Value as string ?? MaterialOptions[0];
        var colorName = (row.Cells[ColorColumnIndex].Value as string ?? "").Trim();

        if (row.Tag is int id)
        {
            if (colorName.Length == 0)
            {
                return;
            }
            await _catalogRepository!.UpdateCountertopColorAsync(id, material, colorName);
        }
        else if (colorName.Length > 0)
        {
            var newId = await _catalogRepository!.AddCountertopColorAsync(material, colorName);
            row.Tag = newId;
        }
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Remove")
        {
            return;
        }

        var row = grid.Rows[e.RowIndex];
        if (row.Tag is int id)
        {
            var colorName = row.Cells[ColorColumnIndex].Value as string ?? "";
            var confirm = MessageBox.Show(FindForm(), $"Delete \"{colorName}\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
            await _catalogRepository!.DeleteCountertopColorAsync(id);
        }

        grid.Rows.RemoveAt(e.RowIndex);
    }
}
