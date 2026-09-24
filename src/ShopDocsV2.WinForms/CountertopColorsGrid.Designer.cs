namespace ShopDocsV2.WinForms;

partial class CountertopColorsGrid
{
    private System.ComponentModel.IContainer components = null;

    private DataGridView grid = null!;
    private Button addButton = null!;

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
        grid = new DataGridView();
        addButton = new Button();

        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();

        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        var materialColumn = new DataGridViewComboBoxColumn
        {
            Name = "Material",
            HeaderText = "Material",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        };

        var colorColumn = new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "Color / Pattern" };
        var removeColumn = CatalogGrid.CreateRemoveColumn();
        grid.Columns.AddRange([materialColumn, colorColumn, removeColumn]);

        addButton.Dock = DockStyle.Bottom;
        addButton.Text = "+ Add";
        addButton.AutoSize = true;

        Controls.Add(grid);
        Controls.Add(addButton);
        AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
