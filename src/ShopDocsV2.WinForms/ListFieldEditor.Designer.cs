namespace ShopDocsV2.WinForms;

partial class ListFieldEditor
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
        grid.AllowUserToDeleteRows = true;
        grid.RowHeadersVisible = true;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        addButton.Dock = DockStyle.Bottom;
        addButton.Text = "+ Add";
        addButton.AutoSize = true;
        addButton.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(grid);
        Controls.Add(addButton);
        AutoScaleMode = AutoScaleMode.Font;
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}
