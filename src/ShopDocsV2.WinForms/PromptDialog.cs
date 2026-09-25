namespace ShopDocsV2.WinForms;

/// <summary>Minimal single-field input dialog (WinForms has no built-in InputBox in C#).</summary>
internal static class PromptDialog
{
    public static string? ShowInput(IWin32Window? owner, string title, string label, string initialValue)
    {
        using var form = FixedDialog.Create(title, 320, 110);

        var labelControl = new Label { Text = label, AutoSize = true, Location = new Point(12, 15) };
        var textBox = new TextBox { Text = initialValue, Location = new Point(12, 35), Width = 296 };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(152, 70), Width = 75 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(233, 70), Width = 75 };

        form.Controls.Add(labelControl);
        form.Controls.Add(textBox);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        textBox.SelectAll();
        return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text : null;
    }
}
