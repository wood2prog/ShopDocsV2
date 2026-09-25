namespace ShopDocsV2.WinForms;

partial class CatalogManagerForm
{
    private System.ComponentModel.IContainer components = null;

    private TabControl tabControl = null!;
    private TabPage materialsTabPage = null!;
    private TabPage finishesTabPage = null!;
    private TabPage countertopColorsTabPage = null!;
    private TabPage pullsTabPage = null!;
    private TabPage hardwareColorsTabPage = null!;

    private SimpleCatalogGrid materialsGrid = null!;
    private FinishesGrid finishesGrid = null!;
    private CountertopColorsGrid countertopColorsGrid = null!;
    private SimpleCatalogGrid pullsGrid = null!;
    private SimpleCatalogGrid hardwareColorsGrid = null!;

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

        tabControl = new TabControl();
        materialsTabPage = new TabPage("Materials");
        finishesTabPage = new TabPage("Finishes");
        countertopColorsTabPage = new TabPage("Countertop Colors");
        pullsTabPage = new TabPage("Pulls");
        hardwareColorsTabPage = new TabPage("Hardware Colors");

        materialsGrid = new SimpleCatalogGrid();
        finishesGrid = new FinishesGrid();
        countertopColorsGrid = new CountertopColorsGrid();
        pullsGrid = new SimpleCatalogGrid();
        hardwareColorsGrid = new SimpleCatalogGrid();

        SuspendLayout();

        materialsTabPage.Controls.Add(materialsGrid);
        finishesTabPage.Controls.Add(finishesGrid);
        countertopColorsTabPage.Controls.Add(countertopColorsGrid);
        pullsTabPage.Controls.Add(pullsGrid);
        hardwareColorsTabPage.Controls.Add(hardwareColorsGrid);

        tabControl.Dock = DockStyle.Fill;
        tabControl.TabPages.AddRange(
        [
            materialsTabPage,
            finishesTabPage,
            countertopColorsTabPage,
            pullsTabPage,
            hardwareColorsTabPage
        ]);

        Controls.Add(tabControl);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(700, 500);
        MinimumSize = new Size(500, 350);
        Text = "Catalog Manager";
        StartPosition = FormStartPosition.CenterParent;

        ResumeLayout(false);
    }
}
