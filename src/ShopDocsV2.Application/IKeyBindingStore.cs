namespace ShopDocsV2.Application;

/// <summary>
/// Persists the user's key-binding overrides, keyed by command id (e.g. "room.add", "group.countertops").
/// Values are shortcut text such as "Ctrl+F"; an empty string means the user removed that command's shortcut.
/// Commands missing from the map use their default.
/// </summary>
public interface IKeyBindingStore
{
    IReadOnlyDictionary<string, string> Load();

    /// <summary>Replaces all stored overrides. Throws IOException/UnauthorizedAccessException if they can't be written.</summary>
    void Save(IReadOnlyDictionary<string, string> bindings);
}
