using System.Drawing;
using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Small dialog for manually setting a finish's swatch color as either hex or RGB. The two fields
/// stay in sync (typing a valid value in one fills the other) and a preview shows the result.
/// Saving with both fields empty clears the color.
/// </summary>
internal static class ColorEditDialog
{
    /// <summary>Returns the new "#RRGGBB" value, "" to clear the color, or null if the user cancelled.</summary>
    public static string? Show(IWin32Window? owner, string finishName, string? currentHex)
    {
        using var form = new Form
        {
            Text = $"Edit Color - {finishName}",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(320, 190),
            AutoScaleMode = AutoScaleMode.Dpi
        };

        var hexLabel = new Label { Text = "Hex:", AutoSize = true, Location = new Point(12, 16) };
        var hexTextBox = new TextBox { Location = new Point(60, 12), Width = 248, PlaceholderText = "e.g. #EDEAE0" };
        var rgbLabel = new Label { Text = "RGB:", AutoSize = true, Location = new Point(12, 48) };
        var rgbTextBox = new TextBox { Location = new Point(60, 44), Width = 248, PlaceholderText = "e.g. 237, 234, 224" };
        var preview = new Label
        {
            Location = new Point(60, 78),
            Size = new Size(248, 40),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var errorLabel = new Label { AutoSize = true, Location = new Point(12, 126), ForeColor = Color.Firebrick };
        var pickButton = new Button { Text = "Pick from Screen", Location = new Point(12, 152), Width = 120 };
        var saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(152, 152), Width = 75 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(233, 152), Width = 75 };

        form.Controls.AddRange([hexLabel, hexTextBox, rgbLabel, rgbTextBox, preview, errorLabel, pickButton, saveButton, cancelButton]);
        form.AcceptButton = saveButton;
        form.CancelButton = cancelButton;

        string? result = null;
        var syncing = false;

        void UpdateState(string? hex, string error)
        {
            result = hex;
            errorLabel.Text = error;
            saveButton.Enabled = hex is not null;

            if (!string.IsNullOrEmpty(hex))
            {
                preview.BackColor = ColorTranslator.FromHtml(hex);
                preview.ForeColor = ColorTranslator.FromHtml(ColorMath.GetContrastingTextColor(hex));
                preview.Text = ColorMath.ToRgbLabel(hex);
            }
            else
            {
                preview.BackColor = SystemColors.Control;
                preview.ForeColor = SystemColors.GrayText;
                preview.Text = hex is null ? "" : "No color";
            }
        }

        // Validates the field the user just typed in; if valid, mirrors it into the other field.
        void OnFieldChanged(TextBox source, TextBox other, Func<string, string?> parse, Func<string, string> format, string error)
        {
            if (syncing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(source.Text))
            {
                syncing = true;
                other.Text = "";
                syncing = false;
                UpdateState("", "");
                return;
            }

            var hex = parse(source.Text);
            if (hex is null)
            {
                UpdateState(null, error);
                return;
            }

            syncing = true;
            other.Text = format(hex);
            syncing = false;
            UpdateState(hex, "");
        }

        hexTextBox.TextChanged += (_, _) => OnFieldChanged(hexTextBox, rgbTextBox, s => ColorMath.ParseHexInput(s),
            hex => ColorMath.ToRgbLabel(hex).Replace("RGB ", ""), "Hex must be 3 or 6 digits, e.g. #EDEAE0.");
        rgbTextBox.TextChanged += (_, _) => OnFieldChanged(rgbTextBox, hexTextBox, s => ColorMath.ParseRgbInput(s),
            hex => hex, "RGB must be three numbers 0-255, e.g. 237, 234, 224.");

        pickButton.Click += async (_, _) =>
        {
            // Hide this dialog (Opacity, since hiding a modal form would close it) so it isn't in the
            // screenshot, and give the desktop a moment to repaint underneath before capturing.
            form.Opacity = 0;
            await Task.Delay(200);
            var picked = ScreenColorPicker.PickColor();
            form.Opacity = 1;
            form.Activate();

            if (picked is { } color)
            {
                // Filling the hex field syncs the RGB field and preview via its TextChanged handler.
                hexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        };

        // Seeding the hex field fills in the RGB field and preview via the handler above.
        hexTextBox.Text = ColorMath.ParseHexInput(currentHex) ?? "";
        UpdateState(ColorMath.ParseHexInput(currentHex) ?? "", "");
        hexTextBox.SelectAll();

        return form.ShowDialog(owner) == DialogResult.OK ? result : null;
    }
}
