using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class CatalogManagerForm : Form
{
    private const int MaxVisibleSearchResults = 12;

    private readonly ICatalogRepository _catalogRepository;
    private readonly IPaintColorLookupService _paintColorLookupService;
    private readonly QuestionSet _questionSet;

    public CatalogManagerForm(ICatalogRepository catalogRepository, IPaintColorLookupService paintColorLookupService, QuestionSet questionSet)
    {
        _catalogRepository = catalogRepository;
        _paintColorLookupService = paintColorLookupService;
        _questionSet = questionSet;
        InitializeComponent();

        Load += async (_, _) => await LoadAllAsync();

        searchTextBox.TextChanged += (_, _) => UpdateSearchResults();
        searchTextBox.KeyDown += SearchTextBox_KeyDown;
        searchTextBox.Leave += (_, _) => BeginInvoke(HideSearchResultsUnlessFocused);
        searchResultsListBox.Leave += (_, _) => BeginInvoke(HideSearchResultsUnlessFocused);
        searchResultsListBox.MouseClick += SearchResultsListBox_MouseClick;
        Resize += (_, _) => HideSearchResults();
    }

    private async Task LoadAllAsync()
    {
        await materialsGrid.BindAsync(_catalogRepository, CatalogList.Materials);
        await pullsGrid.BindAsync(_catalogRepository, CatalogList.Pulls);
        await hardwareColorsGrid.BindAsync(_catalogRepository, CatalogList.HardwareColors);
        await finishesGrid.BindAsync(_catalogRepository, _paintColorLookupService);
        await countertopColorsGrid.BindAsync(_catalogRepository, _questionSet);
        await accessoriesGrid.BindAsync(_catalogRepository);
    }

    /// <summary>A catalog row whose searched column contains the search text.</summary>
    private sealed record SearchHit(TabPage Tab, DataGridView Grid, int RowIndex, string ColumnName, string Display)
    {
        public override string ToString() => Display;
    }

    /// <summary>
    /// Every row, across all tabs, whose name (or accessory model number) contains the search text (values starting with it first).
    /// Reads the grids rather than the database, since they hold edits the moment they're made.
    /// </summary>
    private List<SearchHit> FindSearchHits(string text)
    {
        // Searched columns, then columns whose value is shown in brackets to tell similar hits apart
        // (countertop colors repeat across materials; an accessory hit shows its model number or name).
        var sources = new (TabPage Tab, DataGridView Grid, string[] Columns, string[] DetailColumns)[]
        {
            (materialsTabPage, materialsGrid.Grid, ["Name"], []),
            (finishesTabPage, finishesGrid.Grid, ["Name"], []),
            (countertopColorsTabPage, countertopColorsGrid.Grid, ["Color"], ["Material"]),
            (pullsTabPage, pullsGrid.Grid, ["Name"], []),
            (hardwareColorsTabPage, hardwareColorsGrid.Grid, ["Name"], []),
            (accessoriesTabPage, accessoriesGrid.Grid, ["Name", "Model"], ["Model", "Name"])
        };

        var hits = new List<(SearchHit Hit, bool IsPrefix)>();
        foreach (var (tab, grid, columns, detailColumns) in sources)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                string CellText(string column) => row.Cells[column].Value as string ?? "";

                var columnName = columns.FirstOrDefault(c => CellText(c).Contains(text, StringComparison.OrdinalIgnoreCase));
                if (columnName is null)
                {
                    continue;
                }

                var value = CellText(columnName);
                var detail = detailColumns.Where(c => c != columnName).Select(CellText).FirstOrDefault(d => d.Length > 0);
                var label = detail is null ? value : $"{value} ({detail})";
                hits.Add((new SearchHit(tab, grid, row.Index, columnName, $"{label}  —  {tab.Text}"),
                    value.StartsWith(text, StringComparison.OrdinalIgnoreCase)));
            }
        }

        return hits.OrderByDescending(h => h.IsPrefix).Select(h => h.Hit).ToList();
    }

    private void UpdateSearchResults()
    {
        var text = searchTextBox.Text.Trim();
        var hits = text.Length == 0 ? [] : FindSearchHits(text);
        if (hits.Count == 0)
        {
            HideSearchResults();
            return;
        }

        searchResultsListBox.BeginUpdate();
        searchResultsListBox.Items.Clear();
        searchResultsListBox.Items.AddRange([.. hits]);
        searchResultsListBox.SelectedIndex = 0;
        searchResultsListBox.EndUpdate();

        var textBoxBounds = RectangleToClient(searchTextBox.RectangleToScreen(searchTextBox.ClientRectangle));
        var border = searchResultsListBox.Height - searchResultsListBox.ClientSize.Height;
        searchResultsListBox.SetBounds(textBoxBounds.Left, textBoxBounds.Bottom + 2, textBoxBounds.Width,
            searchResultsListBox.ItemHeight * Math.Min(hits.Count, MaxVisibleSearchResults) + border);
        searchResultsListBox.Visible = true;
        searchResultsListBox.BringToFront();
    }

    private void HideSearchResults() => searchResultsListBox.Visible = false;

    private void HideSearchResultsUnlessFocused()
    {
        if (!searchTextBox.Focused && !searchResultsListBox.Focused)
        {
            HideSearchResults();
        }
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        var count = searchResultsListBox.Items.Count;
        if (!searchResultsListBox.Visible || count == 0)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Down:
                searchResultsListBox.SelectedIndex = Math.Min(searchResultsListBox.SelectedIndex + 1, count - 1);
                break;
            case Keys.Up:
                searchResultsListBox.SelectedIndex = Math.Max(searchResultsListBox.SelectedIndex - 1, 0);
                break;
            case Keys.Enter:
                GoToSearchHit(searchResultsListBox.SelectedItem as SearchHit ?? (SearchHit)searchResultsListBox.Items[0]);
                break;
            case Keys.Escape:
                HideSearchResults();
                break;
            default:
                return;
        }

        e.SuppressKeyPress = true;
    }

    private void SearchResultsListBox_MouseClick(object? sender, MouseEventArgs e)
    {
        var index = searchResultsListBox.IndexFromPoint(e.Location);
        if (index >= 0 && searchResultsListBox.Items[index] is SearchHit hit)
        {
            GoToSearchHit(hit);
        }
    }

    private void GoToSearchHit(SearchHit hit)
    {
        HideSearchResults();
        tabControl.SelectedTab = hit.Tab;
        if (hit.RowIndex < hit.Grid.Rows.Count)
        {
            CatalogGrid.ShowRow(hit.Grid, hit.RowIndex, hit.ColumnName);
        }
    }
}
