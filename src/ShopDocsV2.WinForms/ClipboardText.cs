using System.Runtime.InteropServices;

namespace ShopDocsV2.WinForms;

internal static class ClipboardText
{
    /// <summary>Copies text to the clipboard. If another program is holding the clipboard open, says so and returns false instead of throwing.</summary>
    public static bool TrySet(IWin32Window? owner, string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            MessageBox.Show(owner, "The clipboard is in use by another program. Please try again.",
                "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }
}
