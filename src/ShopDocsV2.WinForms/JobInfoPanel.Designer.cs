namespace ShopDocsV2.WinForms;

partial class JobInfoPanel
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel layoutPanel = null!;
    private TextBox customerNameTextBox = null!;
    private TextBox customerPhoneTextBox = null!;
    private TextBox customerEmailTextBox = null!;
    private TextBox addressTextBox = null!;
    private DateTimePicker dateCreatedPicker = null!;
    private FlowLayoutPanel dueDateFlowPanel = null!;
    private CheckBox dueDateCheckBox = null!;
    private DateTimePicker dueDatePicker = null!;

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

        layoutPanel = new TableLayoutPanel();
        customerNameTextBox = new TextBox();
        customerPhoneTextBox = new TextBox();
        customerEmailTextBox = new TextBox();
        addressTextBox = new TextBox();
        dateCreatedPicker = new DateTimePicker();
        dueDateFlowPanel = new FlowLayoutPanel();
        dueDateCheckBox = new CheckBox();
        dueDatePicker = new DateTimePicker();

        SuspendLayout();

        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.ColumnCount = 2;
        layoutPanel.RowCount = 6;
        layoutPanel.Padding = new Padding(8);
        layoutPanel.AutoSize = true;
        layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        }

        AddRow(0, "Customer Name", customerNameTextBox);
        AddRow(1, "Phone", customerPhoneTextBox);
        AddRow(2, "Email", customerEmailTextBox);
        AddRow(3, "Address", addressTextBox);

        dateCreatedPicker.Format = DateTimePickerFormat.Short;
        dateCreatedPicker.Width = 150;
        AddRow(4, "Date Created", dateCreatedPicker);

        dueDateCheckBox.Text = "Set";
        dueDateCheckBox.AutoSize = true;
        dueDatePicker.Format = DateTimePickerFormat.Short;
        dueDatePicker.Width = 150;
        dueDateFlowPanel.FlowDirection = FlowDirection.LeftToRight;
        dueDateFlowPanel.AutoSize = true;
        dueDateFlowPanel.WrapContents = false;
        dueDateFlowPanel.Controls.Add(dueDateCheckBox);
        dueDateFlowPanel.Controls.Add(dueDatePicker);
        AddRow(5, "Due Date", dueDateFlowPanel);

        Controls.Add(layoutPanel);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        ResumeLayout(false);
        PerformLayout();
    }

    private void AddRow(int row, string labelText, Control control)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };
        if (control is TextBox)
        {
            control.Dock = DockStyle.Fill;
        }
        else
        {
            control.Anchor = AnchorStyles.Left;
        }
        layoutPanel.Controls.Add(label, 0, row);
        layoutPanel.Controls.Add(control, 1, row);
    }
}
