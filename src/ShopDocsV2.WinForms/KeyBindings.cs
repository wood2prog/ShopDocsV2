namespace ShopDocsV2.WinForms;

/// <summary>A menu command whose shortcut can be changed under Tools &gt; Keyboard Shortcuts. The menu item's ShortcutKeys is the live binding.</summary>
internal sealed class KeyBindingCommand(string id, string name, Keys defaultKeys, ToolStripMenuItem menuItem)
{
    /// <summary>Stable key in settings.json, e.g. "room.add" or "group.countertops".</summary>
    public string Id { get; } = id;

    /// <summary>Shown in the Keyboard Shortcuts dialog, e.g. "Room: Add Room".</summary>
    public string Name { get; } = name;

    public Keys DefaultKeys { get; } = defaultKeys;

    public Keys Keys
    {
        get => menuItem.ShortcutKeys;
        set => menuItem.ShortcutKeys = value;
    }
}

/// <summary>Formatting, validation and loading of key bindings.</summary>
internal static class KeyBindings
{
    private static readonly KeysConverter Converter = new();

    /// <summary>Keys the fields and grids need for typing and moving around, whatever modifiers are held.</summary>
    private static readonly HashSet<Keys> EditingKeyCodes =
    [
        Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
        Keys.Back, Keys.Delete, Keys.Insert, Keys.Space, Keys.Enter, Keys.Escape
    ];

    /// <summary>Clipboard/undo shortcuts, grid editing keys (F2 edits a cell, F4 opens a dropdown) and closing the window.</summary>
    private static readonly HashSet<Keys> ReservedShortcuts =
    [
        Keys.Control | Keys.C, Keys.Control | Keys.V, Keys.Control | Keys.X, Keys.Control | Keys.A,
        Keys.Control | Keys.Z, Keys.Control | Keys.Y, Keys.Control | Keys.Shift | Keys.Z,
        Keys.F2, Keys.F4, Keys.Alt | Keys.F4
    ];

    /// <summary>"Ctrl+Shift+Tab" style text; empty for no shortcut.</summary>
    public static string Format(Keys keys) => keys == Keys.None ? "" : Converter.ConvertToInvariantString(keys) ?? "";

    public static bool TryParse(string? text, out Keys keys)
    {
        keys = Keys.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        try
        {
            keys = Converter.ConvertFromInvariantString(text.Trim()) is Keys parsed ? parsed : Keys.None;
            return keys != Keys.None;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Why keys can't be used as a shortcut, or null if it can. Keys.None (no shortcut) is always allowed.</summary>
    public static string? Validate(Keys keys)
    {
        if (keys == Keys.None)
        {
            return null;
        }

        if (EditingKeyCodes.Contains(keys & Keys.KeyCode) || ReservedShortcuts.Contains(keys))
        {
            return $"{Format(keys)} is needed for typing and editing, so it can't be a shortcut.";
        }

        if (!ToolStripManager.IsValidShortcut(keys))
        {
            return $"{Format(keys)} can't be a shortcut. Use Ctrl or Alt with a key, or a function key (F1-F12).";
        }

        return null;
    }

    /// <summary>
    /// Sets each command to its saved override, or its default if it has none. A shortcut already taken by another
    /// command (saved overrides win over defaults) or no longer valid is left unbound rather than doubled up.
    /// </summary>
    public static void Apply(IReadOnlyList<KeyBindingCommand> commands, IReadOnlyDictionary<string, string> overrides)
    {
        var desired = commands.ToDictionary(c => c, c =>
            overrides.TryGetValue(c.Id, out var text) && TryParse(text, out var keys) ? (Keys: keys, IsOverride: true) : (Keys: c.DefaultKeys, IsOverride: false));

        var taken = new HashSet<Keys>();
        foreach (var command in commands.OrderBy(c => desired[c].IsOverride ? 0 : 1))
        {
            var keys = desired[command].Keys;
            command.Keys = keys != Keys.None && Validate(keys) is null && taken.Add(keys) ? keys : Keys.None;
        }
    }

    /// <summary>The bindings that differ from their defaults, in the form IKeyBindingStore saves.</summary>
    public static Dictionary<string, string> Overrides(IEnumerable<KeyBindingCommand> commands) =>
        commands.Where(c => c.Keys != c.DefaultKeys).ToDictionary(c => c.Id, c => Format(c.Keys));
}
