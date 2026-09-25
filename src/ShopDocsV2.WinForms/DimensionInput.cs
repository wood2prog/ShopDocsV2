namespace ShopDocsV2.WinForms;

/// <summary>
/// Limits a TextBox to measurement characters for QuestionType.Dimension fields: digits, spaces, '/', '.' and '"',
/// so "30", "30 1/2", "30.5" and "30"" work but letters don't. Typed keys are blocked; pasted text is cleaned.
/// </summary>
internal static class DimensionInput
{
    public static bool IsAllowed(char c) => char.IsDigit(c) || c is ' ' or '/' or '.' or '"';

    public static void Attach(TextBox textBox)
    {
        textBox.KeyPress += BlockInvalidKey;
        textBox.TextChanged += StripInvalidText;
    }

    /// <summary>For the grid's shared editing TextBox, which is reused by every text column.</summary>
    public static void Detach(TextBox textBox)
    {
        textBox.KeyPress -= BlockInvalidKey;
        textBox.TextChanged -= StripInvalidText;
    }

    // Control characters (Backspace, Ctrl+C/V/X...) pass through; pastes are handled by StripInvalidText.
    private static void BlockInvalidKey(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !IsAllowed(e.KeyChar))
        {
            e.Handled = true;
        }
    }

    private static void StripInvalidText(object? sender, EventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var cleaned = new string(textBox.Text.Where(IsAllowed).ToArray());
        if (cleaned == textBox.Text)
        {
            return;
        }

        var caret = Math.Max(0, textBox.SelectionStart - (textBox.Text.Length - cleaned.Length));
        textBox.Text = cleaned;
        textBox.SelectionStart = caret;
    }
}
