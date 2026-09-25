namespace ShopDocsV2.WinForms;

partial class AccessoriesGrid
{
    private System.ComponentModel.IContainer components = null;

    private DataGridView grid = null!;
    private Button addButton = null!;
    private Label hintLabel = null!;

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
        hintLabel = new Label();

        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();

        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        var nameColumn = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 100 };
        var modelColumn = new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Model Number", FillWeight = 70 };
        var urlColumn = new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "Link", FillWeight = 130 };
        var openColumn = new DataGridViewLinkColumn
        {
            Name = "Open",
            HeaderText = "",
            Text = "Open",
            UseColumnTextForLinkValue = true,
            Width = 60,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var removeColumn = CatalogGrid.CreateRemoveColumn();
        grid.Columns.AddRange([nameColumn, modelColumn, urlColumn, openColumn, removeColumn]);

        addButton.Dock = DockStyle.Bottom;
        addButton.Text = "+ Add";
        addButton.AutoSize = true;

        hintLabel.Dock = DockStyle.Top;
        hintLabel.Text = "Tip: select a Link cell and press Ctrl+V to paste a web address, then click Open to visit it.";
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.AutoSize = false;
        hintLabel.Height = 20;
        hintLabel.Padding = new Padding(4, 4, 4, 0);

        Controls.Add(grid);
        Controls.Add(hintLabel);
        Controls.Add(addButton);
        AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
