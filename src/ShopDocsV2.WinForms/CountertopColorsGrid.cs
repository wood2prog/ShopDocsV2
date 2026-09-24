using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>Countertop Colors CRUD grid: one row per (material, color), material picked from the options questions.json gives the countertop material field.</summary>
public partial class CountertopColorsGrid : UserControl
{
    private const int MaterialColumnIndex = 0;
    private const int ColorColumnIndex = 1;

    private ICatalogRepository? _catalogRepository;
    private List<string> _materialOptions = [];

    public CountertopColorsGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(ICatalogRepository catalogRepository, QuestionSet questionSet)
    {
        _catalogRepository = catalogRepository;
        _materialOptions = MaterialOptionsFrom(questionSet);
        await ReloadAsync();
    }

    /// <summary>
    /// The options of the field that a "countertops" catalog field is filtered by (countertops.material in the
    /// shop's questions.json), so this list always matches what the room form offers. Empty if the schema has none.
    /// </summary>
    private static List<string> MaterialOptionsFrom(QuestionSet questionSet)
    {
        var fieldLists = questionSet.Sections
            .SelectMany(s => s.Questions)
            .Select(q => q.ItemFields)
            .OfType<List<QuestionDef>>();

        foreach (var fields in fieldLists)
        {
            if (fields.FirstOrDefault(f => f.CatalogSource == "countertops" && f.CatalogFilterBy is not null) is { } colorField)
            {
                return fields.FirstOrDefault(f => f.Id == colorField.CatalogFilterBy)?.Options ?? [];
            }
        }

        return [];
    }

    private async Task ReloadAsync()
    {
        var colors = await _catalogRepository!.GetCountertopColorsAsync();

        // Also offer materials already saved but no longer in questions.json, so those rows still display.
        var materialColumn = (DataGridViewComboBoxColumn)grid.Columns[MaterialColumnIndex];
        materialColumn.Items.Clear();
        materialColumn.Items.AddRange([.. _materialOptions.Union(colors.Select(c => c.MaterialName))]);

        grid.Rows.Clear();
        foreach (var color in colors)
        {
            var rowIndex = grid.Rows.Add(color.MaterialName, color.ColorName);
            grid.Rows[rowIndex].Tag = color.Id;
        }
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        var rowIndex = grid.Rows.Add();
        grid.Rows[rowIndex].Cells[MaterialColumnIndex].Value = _materialOptions.FirstOrDefault();
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
        var material = row.Cells[MaterialColumnIndex].Value as string;
        var colorName = (row.Cells[ColorColumnIndex].Value as string ?? "").Trim();
        if (string.IsNullOrEmpty(material))
        {
            return;
        }

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
