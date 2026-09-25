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
    private ToolTip copyToolTip = null!;

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
        copyToolTip = new ToolTip(components);

        SuspendLayout();

        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.ColumnCount = 3;
        layoutPanel.RowCount = 6;
        layoutPanel.Padding = new Padding(8);
        layoutPanel.AutoSize = true;
        layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
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
        AutoScaleMode = AutoScaleMode.Dpi;
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
            Dock = DockStyle.Fill,
            ForeColor = Theme.Wood
        };
        if (control is TextBox textBox)
        {
            control.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(CreateCopyButton(textBox, labelText), 2, row);
        }
        else
        {
            control.Anchor = AnchorStyles.Left;
        }
        layoutPanel.Controls.Add(label, 0, row);
        layoutPanel.Controls.Add(control, 1, row);
    }

    private Button CreateCopyButton(TextBox source, string labelText)
    {
        var button = new Button
        {
            // U+E8C8 is the "Copy" glyph in Segoe MDL2 Assets (ships with Windows 10/11).
            Text = "",
            Font = new Font("Segoe MDL2 Assets", 9F),
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 2, 0, 2),
            Padding = Padding.Empty,
            TabStop = false,
            AccessibleName = $"Copy {labelText}",
            Tag = source
        };
        Theme.StyleIconButton(button);
        copyToolTip.SetToolTip(button, $"Copy {labelText} to clipboard");
        button.Click += CopyButton_Click;
        return button;
    }
}
