using System.Drawing;
using System.Text.Json;
using ShopDocsV2.Infrastructure.Sqlite;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Remembers a form's position, size, and maximized state across runs in window.json next to the
/// SQLite database. Kept out of settings.json because QuestionSetFileProvider rewrites that file whole.
/// </summary>
internal static class WindowPlacementStore
{
    private static readonly string FilePath = Path.Combine(
        Path.GetDirectoryName(SqliteConnectionFactory.GetDefaultDbPath())!, "window.json");

    /// <summary>Applies the saved placement, if any and still on a connected screen. Call before the form is shown.</summary>
    public static void Restore(Form form)
    {
        Placement? placement;
        try
        {
            placement = File.Exists(FilePath) ? JsonSerializer.Deserialize<Placement>(File.ReadAllText(FilePath)) : null;
        }
        catch
        {
            // Corrupt or unreadable file: keep the default placement.
            return;
        }

        if (placement is null || placement.Width <= 0 || placement.Height <= 0)
        {
            return;
        }

        var bounds = new Rectangle(placement.X, placement.Y, placement.Width, placement.Height);
        // A monitor may have been unplugged since; don't reopen the window somewhere it can't be seen.
        if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds)))
        {
            return;
        }

        form.StartPosition = FormStartPosition.Manual;
        form.Bounds = bounds;
        if (placement.Maximized)
        {
            form.WindowState = FormWindowState.Maximized;
        }
    }

    public static void Save(Form form)
    {
        // RestoreBounds holds the normal-state bounds while maximized or minimized.
        var bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
        var placement = new Placement
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = form.WindowState == FormWindowState.Maximized
        };

        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(placement));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not worth interrupting shutdown over; the window just opens at its default placement next time.
        }
    }

    private sealed class Placement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Maximized { get; set; }
    }
}
