namespace ShopDocsV2.WinForms;

partial class ListFieldEditor
{
    private System.ComponentModel.IContainer components = null;

    private DataGridView grid = null!;
    private Button addButton = null!;
    private FlowLayoutPanel addButtonPanel = null!;
    private Label emptyLabel = null!;

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
        addButtonPanel = new FlowLayoutPanel();
        emptyLabel = new Label();

        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();

        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = true;
        grid.RowHeadersVisible = true;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        Theme.StyleGrid(grid);

        // Sized to its label (with some breathing room) instead of stretching across the group.
        addButton.Text = "+ Add";
        addButton.AutoSize = true;
        addButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        addButton.Padding = new Padding(10, 4, 10, 4);
        addButton.MinimumSize = new Size(110, 0);
        addButton.Margin = new Padding(0, 4, 0, 0);
        Theme.StyleAccentButton(addButton);

        addButtonPanel.Dock = DockStyle.Bottom;
        addButtonPanel.AutoSize = true;
        addButtonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        addButtonPanel.WrapContents = false;
        addButtonPanel.Controls.Add(addButton);

        emptyLabel.Dock = DockStyle.Fill;
        emptyLabel.Text = "None added yet.";
        emptyLabel.ForeColor = SystemColors.GrayText;
        emptyLabel.TextAlign = ContentAlignment.MiddleLeft;
        emptyLabel.Visible = false;

        Controls.Add(grid);
        Controls.Add(emptyLabel);
        Controls.Add(addButtonPanel);
        AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
