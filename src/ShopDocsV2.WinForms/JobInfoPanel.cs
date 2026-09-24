using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class JobInfoPanel : UserControl
{
    private Job? _job;
    private bool _loading;

    public event EventHandler? FieldsChanged;

    public JobInfoPanel()
    {
        InitializeComponent();

        customerNameTextBox.TextChanged += (_, _) => Update(j => j.CustomerName = customerNameTextBox.Text);
        customerPhoneTextBox.TextChanged += CustomerPhoneTextBox_TextChanged;
        customerEmailTextBox.TextChanged += (_, _) => Update(j => j.CustomerEmail = NullIfEmpty(customerEmailTextBox.Text));
        addressTextBox.TextChanged += (_, _) => Update(j => j.Address = NullIfEmpty(addressTextBox.Text));
        dateCreatedPicker.ValueChanged += (_, _) => Update(j => j.DateCreated = dateCreatedPicker.Value.Date);
        dueDateCheckBox.CheckedChanged += DueDateCheckBox_CheckedChanged;
        dueDatePicker.ValueChanged += (_, _) => Update(j => j.DueDate = dueDatePicker.Value.Date);
    }

    public void LoadJob(Job? job)
    {
        _loading = true;
        _job = job;

        customerNameTextBox.Text = job?.CustomerName ?? "";
        customerPhoneTextBox.Text = job?.CustomerPhone ?? "";
        customerEmailTextBox.Text = job?.CustomerEmail ?? "";
        addressTextBox.Text = job?.Address ?? "";
        dateCreatedPicker.Value = (job?.DateCreated ?? DateTime.Today).Date;
        dueDateCheckBox.Checked = job?.DueDate is not null;
        dueDatePicker.Value = (job?.DueDate ?? DateTime.Today).Date;
        dueDatePicker.Enabled = dueDateCheckBox.Checked;

        _loading = false;
    }

    private void CustomerPhoneTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var formatted = FormatPhoneNumber(customerPhoneTextBox.Text);
        if (formatted != customerPhoneTextBox.Text)
        {
            customerPhoneTextBox.Text = formatted;
            customerPhoneTextBox.SelectionStart = customerPhoneTextBox.Text.Length;
            return;
        }

        Update(j => j.CustomerPhone = NullIfEmpty(formatted));
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: TextBox source } button)
        {
            return;
        }

        var text = source.Text.Trim();
        if (text.Length == 0)
        {
            copyToolTip.Show("Nothing to copy", button, 0, button.Height, 1500);
            return;
        }

        try
        {
            Clipboard.SetText(text);
            copyToolTip.Show("Copied!", button, 0, button.Height, 1500);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // Another process is holding the clipboard open.
            copyToolTip.Show("Clipboard is busy, try again", button, 0, button.Height, 2000);
        }
    }

    private void DueDateCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        dueDatePicker.Enabled = dueDateCheckBox.Checked;
        Update(j => j.DueDate = dueDateCheckBox.Checked ? dueDatePicker.Value.Date : null);
    }

    private void Update(Action<Job> apply)
    {
        if (_loading || _job is null)
        {
            return;
        }

        apply(_job);
        FieldsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>Ported from the original app.js's formatPhoneNumber: progressively masks to (XXX) XXX-XXXX as digits are typed.</summary>
    private static string FormatPhoneNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length > 10)
        {
            digits = digits[..10];
        }

        return digits.Length switch
        {
            < 4 => digits,
            < 7 => $"({digits[..3]}) {digits[3..]}",
            _ => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
        };
    }
}
