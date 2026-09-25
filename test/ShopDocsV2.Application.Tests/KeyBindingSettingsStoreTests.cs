using ShopDocsV2.Infrastructure.Files;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class KeyBindingSettingsStoreTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-keybindings-test-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_MissingFile_ReturnsNoBindings()
    {
        Assert.Empty(new KeyBindingSettingsStore(_settingsPath).Load());
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNoBindings()
    {
        File.WriteAllText(_settingsPath, "{ not valid json");

        Assert.Empty(new KeyBindingSettingsStore(_settingsPath).Load());
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsIncludingRemovedShortcuts()
    {
        var store = new KeyBindingSettingsStore(_settingsPath);
        store.Save(new Dictionary<string, string> { ["group.countertops"] = "Ctrl+Shift+T", ["room.next"] = "" });

        var loaded = new KeyBindingSettingsStore(_settingsPath).Load();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Ctrl+Shift+T", loaded["group.countertops"]);
        Assert.Equal("", loaded["room.next"]);
    }

    [Fact]
    public void Save_ReplacesPreviousBindingsAndKeepsOtherSettings()
    {
        File.WriteAllText(_settingsPath, """{ "QuestionsPath": "C:\\custom\\questions.json" }""");
        var store = new KeyBindingSettingsStore(_settingsPath);
        store.Save(new Dictionary<string, string> { ["room.add"] = "Ctrl+Q" });

        store.Save(new Dictionary<string, string> { ["room.next"] = "F6" });

        var loaded = store.Load();
        Assert.Equal("F6", Assert.Single(loaded).Value);
        using var settings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(_settingsPath));
        Assert.Equal("C:\\custom\\questions.json", settings.RootElement.GetProperty("QuestionsPath").GetString());
    }

    public void Dispose()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }
}
