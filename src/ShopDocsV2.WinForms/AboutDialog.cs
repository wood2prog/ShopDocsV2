using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace ShopDocsV2.WinForms;

/// <summary>
/// About box: app icon, name, version, compile date, a short description, and the copyright.
/// Version comes from &lt;Version&gt; in the csproj; the compile date and copyright year are stamped at build time.
/// </summary>
internal static class AboutDialog
{
    private const string Description =
        "Capture per-room job specs for the cabinet shop (cabinets, countertops, sinks, appliances and more), " +
        "then print them, export them as text, or export a .ordx file for the CV cabinet-design software.";

    public static void Show(IWin32Window? owner)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "unknown";
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        var buildDate = BuildDate(assembly);

        using var form = new Form
        {
            Text = "About Shop Docs",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 210),
            AutoScaleMode = AutoScaleMode.Dpi
        };

        var iconBox = new PictureBox
        {
            Location = new Point(16, 16),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath)?.ToBitmap()
        };
        var titleLabel = new Label
        {
            Text = "Shop Docs",
            Font = new Font(form.Font.FontFamily, 14F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(76, 14)
        };
        var versionLabel = new Label
        {
            Text = buildDate is { } date
                ? $"Version {version}  ·  Built {date.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture)}"
                : $"Version {version}",
            AutoSize = true,
            Location = new Point(78, 46),
            ForeColor = SystemColors.GrayText
        };
        var descriptionLabel = new Label
        {
            Text = Description,
            Location = new Point(78, 76),
            Size = new Size(326, 64)
        };
        var copyrightLabel = new Label
        {
            Text = copyright,
            AutoSize = true,
            Location = new Point(78, 146)
        };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(329, 172), Width = 75 };

        form.Controls.AddRange([iconBox, titleLabel, versionLabel, descriptionLabel, copyrightLabel, okButton]);
        form.AcceptButton = okButton;
        form.CancelButton = okButton;
        form.FormClosed += (_, _) => iconBox.Image?.Dispose();

        form.ShowDialog(owner);
    }

    private static DateTime? BuildDate(Assembly assembly)
    {
        var value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildDate")?.Value;
        return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
