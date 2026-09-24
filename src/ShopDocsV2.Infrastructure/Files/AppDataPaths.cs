namespace ShopDocsV2.Infrastructure.Files;

/// <summary>
/// Where user data (the SQLite database and window.json) lives: Documents\ShopDocsV2, so it can be
/// backed up or synced to another computer. Older versions kept it in %LocalAppData%\ShopDocsV2;
/// on first use, files found there are copied over (not moved, so the old copy remains as a backup).
/// </summary>
public static class AppDataPaths
{
    /// <summary>Files carried over from the legacy folder: the database, its WAL side files, and the window placement.</summary>
    private static readonly string[] MigratedFileNames = ["shopdocs.db", "shopdocs.db-wal", "shopdocs.db-shm", "window.json"];

    private static readonly Lazy<string> LazyDataDirectory = new(CreateDataDirectory);

    public static string DataDirectory => LazyDataDirectory.Value;

    private static string CreateDataDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ShopDocsV2");
        Directory.CreateDirectory(dir);
        MigrateFromLegacyDirectory(dir);
        return dir;
    }

    private static void MigrateFromLegacyDirectory(string dir)
    {
        var legacyDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShopDocsV2");
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
}
