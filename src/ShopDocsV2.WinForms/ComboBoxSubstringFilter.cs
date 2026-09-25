namespace ShopDocsV2.WinForms;

/// <summary>
/// Narrows a free-typing (DropDown-style) ComboBox's list to the options containing the typed text anywhere,
/// not just at the start like WinForms' built-in autocomplete, so a finish can be found by its name without
/// knowing its code. Matches starting with the text are listed first. Tab takes the highlighted match, or the
/// top one if none is highlighted. Opening the list with the arrow or a click shows every option again.
/// </summary>
internal sealed class ComboBoxSubstringFilter
{
    private readonly ComboBox _comboBox;
    private readonly Func<IEnumerable<string>> _allOptions;
    private bool _refilling;

    private ComboBoxSubstringFilter(ComboBox comboBox, Func<IEnumerable<string>> allOptions)
    {
        _comboBox = comboBox;
        _allOptions = allOptions;
    }

    /// <summary>allOptions is read on each keystroke, so it can follow a list that changes after attaching.</summary>
    public static ComboBoxSubstringFilter Attach(ComboBox comboBox, Func<IEnumerable<string>> allOptions)
    {
        var filter = new ComboBoxSubstringFilter(comboBox, allOptions);
        comboBox.AutoCompleteMode = AutoCompleteMode.None;
        comboBox.TextUpdate += filter.ComboBox_TextUpdate;
        comboBox.DropDown += filter.ComboBox_DropDown;
        comboBox.PreviewKeyDown += filter.ComboBox_PreviewKeyDown;
        return filter;
    }

    /// <summary>For editing controls the DataGridView reuses across columns, which may not all want filtering.</summary>
    public void Detach()
    {
        _comboBox.TextUpdate -= ComboBox_TextUpdate;
        _comboBox.DropDown -= ComboBox_DropDown;
        _comboBox.PreviewKeyDown -= ComboBox_PreviewKeyDown;
    }

    private void ComboBox_TextUpdate(object? sender, EventArgs e)
    {
        var text = _comboBox.Text;
        var caret = _comboBox.SelectionStart;
        var all = _allOptions().ToList();
        var matches = text.Length == 0
            ? all
            : all.Where(o => o.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                .Concat(all.Where(o => !o.StartsWith(text, StringComparison.OrdinalIgnoreCase) &&
                    o.Contains(text, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        _refilling = true;
        try
        {
            // Closing and reopening makes the list resize to the new item count.
            _comboBox.DroppedDown = false;
            ReplaceItems(matches);
            if (matches.Count > 0 && text.Length > 0)
            {
                _comboBox.DroppedDown = true;
                // Opening the list hides the mouse pointer until it moves; show it again.
                Cursor.Current = Cursors.Default;
            }
        }
        finally
        {
            _refilling = false;
        }

        // Opening the list and replacing items can overwrite the typed text with a match or select it all.
        RestoreText(text, caret);
    }

    private void ComboBox_DropDown(object? sender, EventArgs e)
    {
        if (_refilling)
        {
            return;
        }

        var text = _comboBox.Text;
        ReplaceItems(_allOptions().ToList());
        RestoreText(text, text.Length);
    }

    private void ComboBox_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode != Keys.Tab || _comboBox.Text.Length == 0 || _comboBox.Items.Count == 0)
        {
            return;
        }

        string picked;
        if (_comboBox.DroppedDown && _comboBox.SelectedIndex >= 0)
        {
            picked = _comboBox.GetItemText(_comboBox.SelectedItem);
        }
        else if (_comboBox.Items.Cast<object>().Any(o =>
            string.Equals(_comboBox.GetItemText(o), _comboBox.Text, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        else
        {
            picked = _comboBox.GetItemText(_comboBox.Items[0]);
        }

        _comboBox.DroppedDown = false;
        _comboBox.Text = picked;
    }

    private void ReplaceItems(List<string> options)
    {
        _comboBox.BeginUpdate();
        try
        {
            _comboBox.SelectedIndex = -1;
            _comboBox.Items.Clear();
            _comboBox.Items.AddRange([.. options]);
        }
        finally
        {
            _comboBox.EndUpdate();
        }
    }

    private void RestoreText(string text, int caret)
    {
        _comboBox.SelectedIndex = -1;
        _comboBox.Text = text;
        _comboBox.SelectionStart = Math.Min(caret, text.Length);
        _comboBox.SelectionLength = 0;
    }
}
