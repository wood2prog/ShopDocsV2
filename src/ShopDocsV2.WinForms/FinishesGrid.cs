using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>Finishes CRUD grid: manual hex entry always available (inline, or via the per-row "Edit" color dialog accepting hex or RGB), plus a per-row "Lookup Hex" button calling the paint-color API.</summary>
public partial class FinishesGrid : UserControl
{
    private const int NameColumnIndex = 0;
    private const int HexColumnIndex = 1;
    private const int RgbColumnIndex = 2;

    private ICatalogRepository? _catalogRepository;
    private IPaintColorLookupService? _paintColorLookupService;

    /// <summary>The rows, for the Catalog Manager's search.</summary>
    internal DataGridView Grid => grid;

    public FinishesGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellClick += Grid_CellClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        grid.CellFormatting += Grid_CellFormatting;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(ICatalogRepository catalogRepository, IPaintColorLookupService paintColorLookupService)
    {
        _catalogRepository = catalogRepository;
        _paintColorLookupService = paintColorLookupService;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var finishes = await _catalogRepository!.GetFinishesAsync();
        grid.Rows.Clear();
        foreach (var finish in finishes)
        {
            var rowIndex = grid.Rows.Add(finish.Name, finish.HexColor ?? "");
            grid.Rows[rowIndex].Tag = finish.Id;
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        var name = PromptDialog.ShowInput(FindForm(), "Add Finish", "Name:", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _catalogRepository!.AddFinishAsync(name.Trim(), null);
        await ReloadAsync();
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
        var name = row.Cells[NameColumnIndex].Value as string ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var hex = row.Cells[HexColumnIndex].Value as string;
        await _catalogRepository!.UpdateFinishAsync(id, name.Trim(), string.IsNullOrWhiteSpace(hex) ? null : hex.Trim());
        grid.InvalidateRow(row.Index);
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        var row = grid.Rows[e.RowIndex];
        var name = row.Cells[NameColumnIndex].Value as string ?? "";

        switch (grid.Columns[e.ColumnIndex].Name)
        {
            case CatalogGrid.RemoveColumnName:
                await RemoveAsync(id, name);
                break;
            case "EditColor":
                await EditColorAsync(row, id, name);
                break;
            case "Lookup":
                await LookupHexAsync(row, id, name);
                break;
        }
    }

    private async Task RemoveAsync(int id, string name)
    {
        if (!CatalogGrid.ConfirmDelete(this, name))
        {
            return;
        }

        await _catalogRepository!.DeleteFinishAsync(id);
        await ReloadAsync();
    }

    private async Task EditColorAsync(DataGridViewRow row, int id, string name)
    {
        var newHex = ColorEditDialog.Show(FindForm(), name, row.Cells[HexColumnIndex].Value as string);
        if (newHex is null)
        {
            return;
        }

        // A later "Lookup Hex" click simply overwrites this manual value.
        row.Cells[HexColumnIndex].Value = newHex;
        await CommitRowAsync(row, id);
    }

    private async Task LookupHexAsync(DataGridViewRow row, int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var hex = await _paintColorLookupService!.LookupHexAsync(name);
        if (hex is null)
        {
            MessageBox.Show(FindForm(), $"No color match found for \"{name}\". You can still enter a hex value manually.",
                "Lookup Hex", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        row.Cells[HexColumnIndex].Value = hex;
        await CommitRowAsync(row, id);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || (e.ColumnIndex != HexColumnIndex && e.ColumnIndex != RgbColumnIndex))
        {
            return;
        }

        var rowHex = grid.Rows[e.RowIndex].Cells[HexColumnIndex].Value as string;

        if (e.ColumnIndex == RgbColumnIndex)
        {
            e.Value = string.IsNullOrEmpty(rowHex) ? "" : ColorMath.ToRgbLabel(rowHex);
            e.FormattingApplied = true;
        }

        if (SwatchColors.TryGet(rowHex, out var background, out var foreground))
        {
            e.CellStyle!.BackColor = background;
            e.CellStyle.ForeColor = foreground;
        }
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        var rowHex = grid.Rows[e.RowIndex].Cells[HexColumnIndex].Value as string;

        string? textToCopy = e.ColumnIndex switch
        {
            HexColumnIndex => string.IsNullOrEmpty(rowHex) ? null : rowHex,
            RgbColumnIndex => string.IsNullOrEmpty(rowHex) ? null : ColorMath.ToRgbLabel(rowHex),
            _ => null
        };

        if (!string.IsNullOrEmpty(textToCopy))
        {
            ClipboardText.TrySet(FindForm(), textToCopy);
        }
    }
}
