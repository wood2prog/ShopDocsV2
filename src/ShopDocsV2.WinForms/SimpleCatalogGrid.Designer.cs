namespace ShopDocsV2.WinForms;

partial class SimpleCatalogGrid
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

        var nameColumn = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name" };
        var removeColumn = new DataGridViewButtonColumn
        {
            Name = "Remove",
            HeaderText = "",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 32,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        grid.Columns.AddRange([nameColumn, removeColumn]);

        addButton.Dock = DockStyle.Bottom;
        addButton.Text = "+ Add";
        addButton.AutoSize = true;

        Controls.Add(grid);
        Controls.Add(addButton);
        AutoScaleMode = AutoScaleMode.Font;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
