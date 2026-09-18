using System.Text.RegularExpressions;

namespace ShopDocsV2.Application;

/// <summary>Pure color-swatch math ported from the original app's textColorForBackground/rgbTextForHex.</summary>
public static partial class ColorMath
{
    [GeneratedRegex(@"^#?([0-9a-fA-F]{6})$")]
    private static partial Regex HexPattern();

    /// <summary>Returns "#111" or "#fff" (contrasting text color for the given background hex), or "" if hex is invalid.</summary>
    public static string GetContrastingTextColor(string? hex)
    {
        if (!TryParseRgb(hex, out var r, out var g, out var b)) return "";

        double Linearize(int c)
        {
            double v = c / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        double luminance = 0.2126 * Linearize(r) + 0.7152 * Linearize(g) + 0.0722 * Linearize(b);
        return luminance > 0.45 ? "#111" : "#fff";
    }

    /// <summary>Returns "RGB r, g, b", or "" if hex is invalid.</summary>
    public static string ToRgbLabel(string? hex)
    {
        if (!TryParseRgb(hex, out var r, out var g, out var b)) return "";
        return $"RGB {r}, {g}, {b}";
    }

    private static bool TryParseRgb(string? hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (hex is null) return false;
        var m = HexPattern().Match(hex);
        if (!m.Success) return false;
        var digits = m.Groups[1].Value;
        r = Convert.ToInt32(digits.Substring(0, 2), 16);
        g = Convert.ToInt32(digits.Substring(2, 2), 16);
        b = Convert.ToInt32(digits.Substring(4, 2), 16);
        return true;
    }
}
