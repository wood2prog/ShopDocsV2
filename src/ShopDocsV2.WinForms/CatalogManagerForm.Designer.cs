namespace ShopDocsV2.WinForms;

partial class CatalogManagerForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel searchPanel = null!;
    private TextBox searchTextBox = null!;
    private ListBox searchResultsListBox = null!;
    private TabControl tabControl = null!;
    private TabPage materialsTabPage = null!;
    private TabPage finishesTabPage = null!;
    private TabPage countertopColorsTabPage = null!;
    private TabPage pullsTabPage = null!;
    private TabPage hardwareColorsTabPage = null!;
    private TabPage accessoriesTabPage = null!;

    private SimpleCatalogGrid materialsGrid = null!;
    private FinishesGrid finishesGrid = null!;
    private CountertopColorsGrid countertopColorsGrid = null!;
    private SimpleCatalogGrid pullsGrid = null!;
    private SimpleCatalogGrid hardwareColorsGrid = null!;
    private AccessoriesGrid accessoriesGrid = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        searchPanel = new Panel();
        searchTextBox = new TextBox();
        searchResultsListBox = new ListBox();
        tabControl = new TabControl();
        materialsTabPage = new TabPage("Materials");
        finishesTabPage = new TabPage("Finishes");
        countertopColorsTabPage = new TabPage("Countertop Colors");
        pullsTabPage = new TabPage("Pulls");
        hardwareColorsTabPage = new TabPage("Hardware Colors");
        accessoriesTabPage = new TabPage("Accessories");

        materialsGrid = new SimpleCatalogGrid();
        finishesGrid = new FinishesGrid();
        countertopColorsGrid = new CountertopColorsGrid();
        pullsGrid = new SimpleCatalogGrid();
        hardwareColorsGrid = new SimpleCatalogGrid();
        accessoriesGrid = new AccessoriesGrid();

        SuspendLayout();

        materialsTabPage.Controls.Add(materialsGrid);
        finishesTabPage.Controls.Add(finishesGrid);
        countertopColorsTabPage.Controls.Add(countertopColorsGrid);
        pullsTabPage.Controls.Add(pullsGrid);
        hardwareColorsTabPage.Controls.Add(hardwareColorsGrid);
        accessoriesTabPage.Controls.Add(accessoriesGrid);

        tabControl.Dock = DockStyle.Fill;
        tabControl.TabPages.AddRange(
        [
            materialsTabPage,
            finishesTabPage,
            countertopColorsTabPage,
            pullsTabPage,
            hardwareColorsTabPage,
            accessoriesTabPage
        ]);

        searchTextBox.Dock = DockStyle.Fill;
        searchTextBox.PlaceholderText = "Search all catalog lists...";

        searchPanel.Dock = DockStyle.Top;
        searchPanel.Padding = new Padding(6);
        searchPanel.Height = searchTextBox.PreferredHeight + searchPanel.Padding.Vertical;
        searchPanel.Controls.Add(searchTextBox);

        // Floats over the tabs just under the search box; positioned and shown by ShowSearchResults.
        searchResultsListBox.Visible = false;
        searchResultsListBox.TabStop = false;
        searchResultsListBox.IntegralHeight = true;

        Controls.Add(tabControl);
        Controls.Add(searchPanel);
        Controls.Add(searchResultsListBox);
        searchResultsListBox.BringToFront();
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(700, 500);
        MinimumSize = new Size(500, 350);
        Text = "Catalog Manager";
        StartPosition = FormStartPosition.CenterParent;

        ResumeLayout(false);
    }
}
