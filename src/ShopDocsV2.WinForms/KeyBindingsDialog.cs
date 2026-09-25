namespace ShopDocsV2.WinForms;

/// <summary>
/// Tools &gt; Keyboard Shortcuts: pick a command, then press the new key combination in the shortcut box.
/// Edits a working copy; the commands (and their menu items) only change on OK.
/// </summary>
internal static class KeyBindingsDialog
{
    /// <summary>Returns true if the user pressed OK, after applying the new bindings to commands.</summary>
    public static bool Show(IWin32Window owner, IReadOnlyList<KeyBindingCommand> commands)
    {
        var working = commands.ToDictionary(c => c, c => c.Keys);

        using var form = FixedDialog.Create("Keyboard Shortcuts", 520, 450);

        var listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Location = new Point(12, 12),
            Size = new Size(496, 300)
        };
        listView.Columns.Add("Command", 330);
        listView.Columns.Add("Shortcut", 140);
        foreach (var command in commands)
        {
            listView.Items.Add(new ListViewItem([command.Name, KeyBindings.Format(working[command])]) { Tag = command });
        }

        var captureLabel = new Label { Text = "New shortcut (press the keys):", AutoSize = true, Location = new Point(12, 325) };
        var captureBox = new ShortcutCaptureBox { Location = new Point(12, 345), Width = 250, Enabled = false };
        var removeButton = new Button { Text = "Remove", Location = new Point(270, 344), Width = 75, Enabled = false };
        var resetButton = new Button { Text = "Reset All", Location = new Point(408, 344), Width = 100 };
        var messageLabel = new Label
        {
            AutoSize = false,
            Location = new Point(12, 375),
            Size = new Size(496, 32),
            ForeColor = Color.Firebrick
        };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(352, 415), Width = 75 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(433, 415), Width = 75 };

        form.Controls.AddRange([listView, captureLabel, captureBox, removeButton, resetButton, messageLabel, okButton, cancelButton]);
        form.CancelButton = cancelButton;

        KeyBindingCommand? Selected() => listView.SelectedItems.Count == 1 ? listView.SelectedItems[0].Tag as KeyBindingCommand : null;

        void RefreshRows()
        {
            foreach (ListViewItem item in listView.Items)
            {
                item.SubItems[1].Text = KeyBindings.Format(working[(KeyBindingCommand)item.Tag!]);
            }
            captureBox.Text = Selected() is { } selected ? KeyBindings.Format(working[selected]) : "";
        }

        void Assign(KeyBindingCommand command, Keys keys)
        {
            messageLabel.Text = "";
            if (KeyBindings.Validate(keys) is { } error)
            {
                messageLabel.Text = error;
                return;
            }

            var holder = keys == Keys.None ? null : working.Keys.FirstOrDefault(c => c != command && working[c] == keys);
            if (holder is not null)
            {
                var move = MessageBox.Show(form,
                    $"{KeyBindings.Format(keys)} is already used by \"{holder.Name}\".\n\nMove it to \"{command.Name}\"?",
                    "Keyboard Shortcuts", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (move != DialogResult.Yes)
                {
                    return;
                }
                working[holder] = Keys.None;
            }

            working[command] = keys;
            RefreshRows();
        }

        listView.SelectedIndexChanged += (_, _) =>
        {
            var hasSelection = Selected() is not null;
            captureBox.Enabled = hasSelection;
            removeButton.Enabled = hasSelection;
            messageLabel.Text = "";
            RefreshRows();
        };
        // Selecting a command puts the cursor in the shortcut box, so the next key press rebinds it.
        listView.ItemActivate += (_, _) => captureBox.Focus();
        listView.MouseClick += (_, _) => captureBox.Focus();

        captureBox.ShortcutPressed += keys =>
        {
            if (Selected() is { } command)
            {
                Assign(command, keys);
            }
        };
        removeButton.Click += (_, _) =>
        {
            if (Selected() is { } command)
            {
                Assign(command, Keys.None);
            }
        };
        resetButton.Click += (_, _) =>
        {
            foreach (var command in commands)
            {
                working[command] = command.DefaultKeys;
            }
            messageLabel.Text = "";
            RefreshRows();
        };

        if (listView.Items.Count > 0)
        {
            listView.Items[0].Selected = true;
        }

        if (form.ShowDialog(owner) != DialogResult.OK)
        {
            return false;
        }

        // Clear first so moving a shortcut between commands never has two menu items holding it at once.
        foreach (var command in commands)
        {
            command.Keys = Keys.None;
        }
        foreach (var command in commands)
        {
            command.Keys = working[command];
        }
        return true;
    }

    /// <summary>A read-only box that reports Ctrl/Alt combinations and function keys instead of typing them. Plain Tab, Enter and Esc still work as usual.</summary>
    private sealed class ShortcutCaptureBox : TextBox
    {
        public event Action<Keys>? ShortcutPressed;

        public ShortcutCaptureBox()
        {
            ReadOnly = true;
            BackColor = SystemColors.Window;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var keyCode = keyData & Keys.KeyCode;
            if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            {
                return true;
            }

            var hasModifier = (keyData & (Keys.Control | Keys.Alt)) != 0;
            var isFunctionKey = keyCode is >= Keys.F1 and <= Keys.F24;
            if (!hasModifier && !isFunctionKey)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            ShortcutPressed?.Invoke(keyData);
            return true;
        }
    }
}
