namespace ShopDocsV2.WinForms;

/// <summary>Reusable add/rename/delete grid for the five catalog kinds that are just an id/name/sort_order table.</summary>
public partial class SimpleCatalogGrid : UserControl
{
    private const string RemoveColumnName = "Remove";

    private Func<CancellationToken, Task<List<(int Id, string Name)>>>? _list;
    private Func<string, CancellationToken, Task<int>>? _add;
    private Func<int, string, CancellationToken, Task>? _update;
    private Func<int, CancellationToken, Task>? _delete;

    public SimpleCatalogGrid()
    {
        InitializeComponent();
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellEndEdit += Grid_CellEndEdit;
        addButton.Click += AddButton_Click;
    }

    internal async Task BindAsync(
        Func<CancellationToken, Task<List<(int Id, string Name)>>> list,
        Func<string, CancellationToken, Task<int>> add,
        Func<int, string, CancellationToken, Task> update,
        Func<int, CancellationToken, Task> delete)
    {
        _list = list;
        _add = add;
        _update = update;
        _delete = delete;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var items = await _list!(CancellationToken.None);
        grid.Rows.Clear();
        foreach (var (id, name) in items)
        {
            var rowIndex = grid.Rows.Add(name);
            grid.Rows[rowIndex].Tag = id;
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        var name = PromptDialog.ShowInput(FindForm(), "Add", "Name:", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _add!(name.Trim(), CancellationToken.None);
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

        await _update!(id, name.Trim(), CancellationToken.None);
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != RemoveColumnName)
        {
            return;
        }

        if (grid.Rows[e.RowIndex].Tag is not int id)
        {
            return;
        }

        var name = grid.Rows[e.RowIndex].Cells[0].Value as string ?? "";
        var confirm = MessageBox.Show(FindForm(), $"Delete \"{name}\"?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await _delete!(id, CancellationToken.None);
        await ReloadAsync();
    }
}
