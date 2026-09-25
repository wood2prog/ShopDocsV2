using System.Diagnostics;

namespace ShopDocsV2.WinForms;

/// <summary>Opens pasted web addresses (accessory links) in the browser.</summary>
internal static class WebLink
{
    /// <summary>
    /// Opens a web address, adding https:// if it was pasted without one. Anything that isn't http(s) is
    /// refused with a message, so a pasted file path can't launch a program.
    /// </summary>
    public static void Open(IWin32Window? owner, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            MessageBox.Show(owner, "This accessory has no link yet. Paste one into its Link cell first.",
                "Open Link", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!text.Contains("://"))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show(owner, $"\"{text}\" isn't a web address (it should start with http:// or https://).",
                "Open Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
