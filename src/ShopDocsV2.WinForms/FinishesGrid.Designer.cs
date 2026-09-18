namespace ShopDocsV2.WinForms;

partial class FinishesGrid
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

        var nameColumn = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name" };
        var hexColumn = new DataGridViewTextBoxColumn { Name = "Hex", HeaderText = "Hex Color" };
        var rgbColumn = new DataGridViewTextBoxColumn
        {
            Name = "Rgb",
            HeaderText = "RGB",
            ReadOnly = true,
            Width = 110,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var lookupColumn = new DataGridViewButtonColumn
        {
            Name = "Lookup",
            HeaderText = "",
            Text = "Lookup Hex",
            UseColumnTextForButtonValue = true,
            Width = 100,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var removeColumn = new DataGridViewButtonColumn
        {
            Name = "Remove",
            HeaderText = "",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 32,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        grid.Columns.AddRange([nameColumn, hexColumn, rgbColumn, lookupColumn, removeColumn]);

        addButton.Dock = DockStyle.Bottom;
        addButton.Text = "+ Add";
        addButton.AutoSize = true;

        hintLabel.Dock = DockStyle.Top;
        hintLabel.Text = "Tip: click a Hex or RGB value to copy it to the clipboard.";
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.AutoSize = false;
        hintLabel.Height = 20;
        hintLabel.Padding = new Padding(4, 4, 4, 0);

        Controls.Add(grid);
        Controls.Add(hintLabel);
        Controls.Add(addButton);
        AutoScaleMode = AutoScaleMode.Font;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
