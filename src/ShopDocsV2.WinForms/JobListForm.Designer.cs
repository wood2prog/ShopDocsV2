namespace ShopDocsV2.WinForms;

partial class JobListForm
{
    private System.ComponentModel.IContainer components = null;

    private TextBox filterTextBox = null!;
    private DataGridView grid = null!;
    private FlowLayoutPanel buttonPanel = null!;
    private Button openButton = null!;
    private Button deleteButton = null!;
    private Button cancelButton = null!;

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

        filterTextBox = new TextBox();
        grid = new DataGridView();
        buttonPanel = new FlowLayoutPanel();
        openButton = new Button();
        deleteButton = new Button();
        cancelButton = new Button();

        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();

        // filterTextBox
        filterTextBox.Dock = DockStyle.Top;
        filterTextBox.PlaceholderText = "Filter by customer or address...";
        filterTextBox.Margin = new Padding(8);

        // grid
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add("Customer", "Customer");
        grid.Columns.Add("Address", "Address");
        grid.Columns.Add("DateCreated", "Date Created");
        grid.Columns.Add("DueDate", "Due Date");
        grid.Columns.Add("Updated", "Updated");

        // buttonPanel
        buttonPanel.Dock = DockStyle.Bottom;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.AutoSize = true;
        buttonPanel.Padding = new Padding(8);

        cancelButton.Text = "Close";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.AutoSize = true;

        deleteButton.Text = "Delete";
        deleteButton.AutoSize = true;

        openButton.Text = "Open";
        openButton.AutoSize = true;

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(openButton);

        // JobListForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(700, 450);
        Controls.Add(grid);
        Controls.Add(filterTextBox);
        Controls.Add(buttonPanel);
        Text = "Open Job";
        StartPosition = FormStartPosition.CenterParent;
        CancelButton = cancelButton;
        MinimumSize = new Size(500, 300);

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
