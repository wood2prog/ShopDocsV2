using System.Drawing;
using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>Turns a stored finish hex into the colors for drawing its swatch.</summary>
internal static class SwatchColors
{
    /// <summary>
    /// Background is the hex color itself, foreground the contrasting text color. Accepts anything
    /// ColorMath.ParseHexInput does ("#EDEAE0", "edeae0", "#EEE"); false for blank or invalid input.
    /// </summary>
    public static bool TryGet(string? hex, out Color background, out Color foreground)
    {
        if (ColorMath.ParseHexInput(hex) is not { } normalized)
        {
            background = foreground = default;
            return false;
        }

        background = ColorTranslator.FromHtml(normalized);
        foreground = ColorTranslator.FromHtml(ColorMath.GetContrastingTextColor(normalized));
        return true;
    }
}
