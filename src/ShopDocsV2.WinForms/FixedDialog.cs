namespace ShopDocsV2.WinForms;

/// <summary>Window setup shared by the small code-built dialogs (PromptDialog, ColorEditDialog, AboutDialog, KeyBindingsDialog).</summary>
internal static class FixedDialog
{
    /// <summary>A non-resizable dialog centered on its owner, with no minimize/maximize buttons or taskbar entry.</summary>
    public static Form Create(string title, int width, int height) => new()
    {
        Text = title,
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MinimizeBox = false,
        MaximizeBox = false,
        ShowInTaskbar = false,
        ClientSize = new Size(width, height),
        AutoScaleMode = AutoScaleMode.Dpi
    };
}
