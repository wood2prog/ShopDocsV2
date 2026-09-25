using System.Text.Json;
using System.Text.Json.Serialization;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Infrastructure.Files;

public sealed class QuestionSetFileProvider(string? settingsFilePath = null) : IQuestionSetProvider
{
    private const string DefaultFileName = "questions.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _settingsFilePath = settingsFilePath ?? AppDataPaths.SettingsFilePath;

    public async Task<QuestionSet> LoadAsync(string? explicitPath = null, CancellationToken ct = default)
    {
        var path = explicitPath ?? ReadRememberedPath() ?? Path.Combine(AppContext.BaseDirectory, DefaultFileName);

        QuestionSetDto dto;
        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            dto = JsonSerializer.Deserialize<QuestionSetDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("File contained no data.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            throw new QuestionSetLoadException(path, $"Could not load question set from '{path}': {ex.Message}", ex);
        }

        if (explicitPath is not null)
        {
            RememberPath(explicitPath);
        }

        return MapToDomain(dto);
    }

    private string? ReadRememberedPath()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return null;
            }

            var settings = JsonSerializer.Deserialize<AppSettingsDto>(File.ReadAllText(_settingsFilePath), JsonOptions);
            return string.IsNullOrWhiteSpace(settings?.QuestionsPath) ? null : settings.QuestionsPath;
        }
        catch
        {
            // Corrupt or unreadable settings file: fall back to the default questions.json location.
            return null;
        }
    }

    private void RememberPath(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(new AppSettingsDto { QuestionsPath = path }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The question set itself loaded fine; it just won't be remembered next time.
        }
    }

    private static QuestionSet MapToDomain(QuestionSetDto dto) => new()
    {
        Sections = dto.Sections.Select(s => new QuestionSection
        {
            Title = s.Title,
            Questions = s.Questions.Select(MapQuestion).ToList()
        }).ToList()
    };

    private static QuestionDef MapQuestion(QuestionDefDto dto) => new()
    {
        Id = dto.Id,
        Label = dto.Label,
        Type = MapType(dto.Type),
        Options = dto.Options,
        Default = dto.Default,
        ShowIf = dto.ShowIf is null ? null : new ShowIfCondition { Field = dto.ShowIf.Field, EqualsValue = dto.ShowIf.EqualsValue },
        CatalogSource = dto.CatalogSource,
        CatalogFilterBy = dto.CatalogFilterBy,
        ColorPreview = dto.ColorPreview,
        Placeholder = dto.Placeholder,
        AddLabel = dto.AddLabel,
        ItemFields = dto.ItemFields?.Select(MapQuestion).ToList(),
        PrintGroup = dto.PrintGroup,
        PrintSuffix = dto.PrintSuffix
    };

    /// <summary>Unknown or absent type strings fall back to Text, mirroring the original app.js's implicit default.</summary>
    private static QuestionType MapType(string? type) => type?.ToLowerInvariant() switch
    {
        "select" => QuestionType.Select,
        "checkbox" => QuestionType.Checkbox,
        "number" => QuestionType.Number,
        "textarea" => QuestionType.Textarea,
        "list" => QuestionType.List,
        "dimension" => QuestionType.Dimension,
        _ => QuestionType.Text
    };

    private sealed class AppSettingsDto
    {
        public string? QuestionsPath { get; set; }
    }

    private sealed class QuestionSetDto
    {
        public List<QuestionSectionDto> Sections { get; set; } = [];
    }

    private sealed class QuestionSectionDto
    {
        public string Title { get; set; } = "";
        public List<QuestionDefDto> Questions { get; set; } = [];
    }

    private sealed class QuestionDefDto
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string? Type { get; set; }
        public List<string>? Options { get; set; }
        public string? Default { get; set; }
        public ShowIfDto? ShowIf { get; set; }
        public string? CatalogSource { get; set; }
        public string? CatalogFilterBy { get; set; }
        public bool ColorPreview { get; set; }
        public string? Placeholder { get; set; }
        public string? AddLabel { get; set; }
        public List<QuestionDefDto>? ItemFields { get; set; }
        public string? PrintGroup { get; set; }
        public string? PrintSuffix { get; set; }
    }

    private sealed class ShowIfDto
    {
        public string Field { get; set; } = "";

        [JsonPropertyName("equals")]
        public string EqualsValue { get; set; } = "";
    }
}
