namespace ShopDocsV2.Domain;

public sealed class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    /// <summary>Scalar (text/number/checkbox) answers, keyed by QuestionDef.Id.</summary>
    public Dictionary<string, AnswerValue> Answers { get; set; } = new();

    /// <summary>List-question item rows, keyed by the list QuestionDef.Id.</summary>
    public Dictionary<string, List<RoomListItem>> ListAnswers { get; set; } = new();

    /// <summary>A copy of this room, its answers and its list items, under new ids.</summary>
    public Room Duplicate() => new()
    {
        Id = Guid.NewGuid(),
        Name = Name,
        SortOrder = SortOrder,
        Answers = new Dictionary<string, AnswerValue>(Answers),
        ListAnswers = ListAnswers.ToDictionary(kv => kv.Key, kv => kv.Value.Select(i => i.Duplicate()).ToList())
    };
}

public sealed class RoomListItem
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Field values for this item, keyed by itemField.Id.</summary>
    public Dictionary<string, AnswerValue> Fields { get; set; } = new();

    public RoomListItem Duplicate() => new()
    {
        Id = Guid.NewGuid(),
        SortOrder = SortOrder,
        Fields = new Dictionary<string, AnswerValue>(Fields)
    };
}

public enum AnswerValueKind { Text, Number, Bool }

public readonly record struct AnswerValue(AnswerValueKind Kind, string? Text, double? Number, bool? Bool)
{
    public static AnswerValue Of(string? s) => new(AnswerValueKind.Text, s, null, null);
    public static AnswerValue Of(double n) => new(AnswerValueKind.Number, null, n, null);
    public static AnswerValue Of(bool b) => new(AnswerValueKind.Bool, null, null, b);

    /// <summary>Plain display text, used for spec formatting and ORDX export.</summary>
    public string DisplayText => Kind switch
    {
        AnswerValueKind.Bool => (Bool ?? false) ? "Yes" : "No",
        AnswerValueKind.Number => Number?.ToString() ?? "",
        _ => Text ?? ""
    };

    /// <summary>Whether this value counts as "filled in" (non-blank text/number, or true for a checkbox).</summary>
    public bool IsBlank => Kind switch
    {
        AnswerValueKind.Bool => !(Bool ?? false),
        AnswerValueKind.Number => Number is null,
        _ => string.IsNullOrWhiteSpace(Text)
    };
}
