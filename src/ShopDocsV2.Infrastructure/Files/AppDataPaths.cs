namespace ShopDocsV2.Infrastructure.Files;

/// <summary>
/// Where user data (the SQLite database, window.json and settings.json) lives: Documents\ShopDocsV2, so it
/// can be backed up or synced to another computer. Older versions kept it in %LocalAppData%\ShopDocsV2;
/// on first use the database and window.json found there are copied over (not moved, so the old copy
/// remains as a backup), and settings.json is moved.
/// </summary>
public static class AppDataPaths
{
    /// <summary>Files carried over from the legacy folder: the database, the WAL side files older versions left beside it, and the window placement.</summary>
    private static readonly string[] MigratedFileNames = ["shopdocs.db", "shopdocs.db-wal", "shopdocs.db-shm", "window.json"];

    private const string SettingsFileName = "settings.json";

    private static readonly string LegacyDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShopDocsV2");

    private static readonly Lazy<string> LazyDataDirectory = new(CreateDataDirectory);

    public static string DataDirectory => LazyDataDirectory.Value;

    /// <summary>App settings: the remembered questions.json path and customized key bindings.</summary>
    public static string SettingsFilePath => Path.Combine(DataDirectory, SettingsFileName);

    private static string CreateDataDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ShopDocsV2");
        Directory.CreateDirectory(dir);
        MigrateFromLegacyDirectory(dir);
        MoveLegacySettings(dir);
        return dir;
    }

    private static void MigrateFromLegacyDirectory(string dir)
    {
        var legacyDir = LegacyDirectory;
        // Only migrate into a fresh folder, so a database already here (e.g. synced from another computer) is never overwritten.
        if (File.Exists(Path.Combine(dir, "shopdocs.db")) || !File.Exists(Path.Combine(legacyDir, "shopdocs.db")))
        {
            return;
        }

        foreach (var name in MigratedFileNames)
        {
            var source = Path.Combine(legacyDir, name);
            var destination = Path.Combine(dir, name);
            if (File.Exists(source) && !File.Exists(destination))
            {
                File.Copy(source, destination);
            }
        }
    }

    /// <summary>
    /// settings.json stayed in the legacy folder until 1.0.28, after the database had already moved, so this runs
    /// on its own rather than only for a fresh folder. Moved rather than copied so a stale copy can't come back later.
    /// </summary>
    private static void MoveLegacySettings(string dir)
    {
        var source = Path.Combine(LegacyDirectory, SettingsFileName);
        var destination = Path.Combine(dir, SettingsFileName);
        if (!File.Exists(source) || File.Exists(destination))
        {
            return;
        }

        try
        {
            File.Move(source, destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not worth failing startup over; the default questions.json is used until a file is picked again.
        }
    }
}
