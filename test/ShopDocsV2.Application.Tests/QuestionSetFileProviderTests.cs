using ShopDocsV2.Application;
using ShopDocsV2.Domain;
using ShopDocsV2.Infrastructure.Files;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class QuestionSetFileProviderTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-questions-test-{Guid.NewGuid():N}.json");
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-settings-test-{Guid.NewGuid():N}.json");
    private readonly QuestionSetFileProvider _provider;

    public QuestionSetFileProviderTests()
    {
        // Isolated from the real Documents\ShopDocsV2\settings.json so this test never touches
        // (or overwrites) the developer's actual remembered questions.json path.
        _provider = new QuestionSetFileProvider(_settingsPath);
    }

    [Fact]
    public async Task LoadAsync_MapsAllQuestionTypesAndDefaultsUnknownTypeToText()
    {
        await File.WriteAllTextAsync(_filePath,
            """
            {
              "sections": [
                {
                  "title": "Cabinets",
                  "questions": [
                    { "id": "cabinet_style", "label": "Cabinet Style", "type": "select", "options": ["Framed", "Frameless"], "default": "Framed" },
                    { "id": "notes_text", "label": "Notes", "type": "textarea" },
                    { "id": "soft_close", "label": "Soft-close", "type": "checkbox" },
                    { "id": "shelf_count", "label": "Shelf Count", "type": "number" },
                    { "id": "cutout_width", "label": "Cutout W", "type": "dimension" },
                    { "id": "no_type_field", "label": "No Type" },
                    { "id": "cabinet_finishes", "label": "Cabinet Finishes", "type": "list", "addLabel": "+ Add",
                      "itemFields": [
                        { "id": "wood", "label": "Wood", "type": "select", "catalogSource": "materials", "printGroup": "Cutout", "printSuffix": "W" }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

        var questionSet = await _provider.LoadAsync(_filePath);

        var section = Assert.Single(questionSet.Sections);
        Assert.Equal("Cabinets", section.Title);
        Assert.Equal(7, section.Questions.Count);

        var byId = section.Questions.ToDictionary(q => q.Id);
        Assert.Equal(QuestionType.Select, byId["cabinet_style"].Type);
        Assert.Equal(["Framed", "Frameless"], byId["cabinet_style"].Options);
        Assert.Equal("Framed", byId["cabinet_style"].Default);
        Assert.Equal(QuestionType.Textarea, byId["notes_text"].Type);
        Assert.Equal(QuestionType.Checkbox, byId["soft_close"].Type);
        Assert.Equal(QuestionType.Number, byId["shelf_count"].Type);
        Assert.Equal(QuestionType.Dimension, byId["cutout_width"].Type);
        Assert.Equal(QuestionType.Text, byId["no_type_field"].Type);

        var listQuestion = byId["cabinet_finishes"];
        Assert.Equal(QuestionType.List, listQuestion.Type);
        Assert.Equal("+ Add", listQuestion.AddLabel);
        var itemField = Assert.Single(listQuestion.ItemFields!);
        Assert.Equal("materials", itemField.CatalogSource);
        Assert.Equal("Cutout", itemField.PrintGroup);
        Assert.Equal("W", itemField.PrintSuffix);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsQuestionSetLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.json");

        var ex = await Assert.ThrowsAsync<QuestionSetLoadException>(() => _provider.LoadAsync(missingPath));
        Assert.Equal(missingPath, ex.Path);
    }

    [Fact]
    public async Task LoadAsync_MalformedJson_ThrowsQuestionSetLoadException()
    {
        await File.WriteAllTextAsync(_filePath, "{ not valid json");

        await Assert.ThrowsAsync<QuestionSetLoadException>(() => _provider.LoadAsync(_filePath));
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }
}
