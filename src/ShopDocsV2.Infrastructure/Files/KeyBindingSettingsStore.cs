using System.Text.Json.Nodes;
using ShopDocsV2.Application;

namespace ShopDocsV2.Infrastructure.Files;

/// <summary>Keeps customized key bindings under "KeyBindings" in settings.json.</summary>
public sealed class KeyBindingSettingsStore(string? settingsFilePath = null) : IKeyBindingStore
{
    private const string PropertyName = "KeyBindings";

    private readonly string _settingsFilePath = settingsFilePath ?? AppDataPaths.SettingsFilePath;

    public IReadOnlyDictionary<string, string> Load()
    {
        var bindings = new Dictionary<string, string>();
        if (SettingsFile.Read(_settingsFilePath)[PropertyName] is not JsonObject stored)
        {
            return bindings;
        }

        foreach (var (commandId, value) in stored)
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var shortcut))
            {
                bindings[commandId] = shortcut;
            }
        }
        return bindings;
    }

    public void Save(IReadOnlyDictionary<string, string> bindings)
    {
        var stored = new JsonObject();
        foreach (var (commandId, shortcut) in bindings.OrderBy(b => b.Key, StringComparer.Ordinal))
        {
            stored[commandId] = shortcut;
        }

        SettingsFile.Update(_settingsFilePath, settings => settings[PropertyName] = stored);
    }
}
