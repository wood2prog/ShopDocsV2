using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShopDocsV2.Infrastructure.Files;

/// <summary>
/// Reads and updates settings.json as a JSON object, so each setting's owner (the remembered questions.json path,
/// key bindings) can change its own property without dropping the others.
/// </summary>
internal static class SettingsFile
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>The settings object, or an empty one if the file is missing, unreadable or corrupt.</summary>
    public static JsonObject Read(string path)
    {
        try
        {
            return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path)) is JsonObject settings ? settings : new JsonObject();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new JsonObject();
        }
    }

    /// <summary>Applies update to the current settings and writes them back. Throws IOException/UnauthorizedAccessException if the write fails.</summary>
    public static void Update(string path, Action<JsonObject> update)
    {
        var settings = Read(path);
        update(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, settings.ToJsonString(WriteOptions));
    }
}
