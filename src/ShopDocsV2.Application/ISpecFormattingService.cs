using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

/// <summary>
/// Pure port of the original app's isAnswered / getRoomSpecSections / buildRoomText.
/// Shared by Print, "Copy Room to Clipboard", and "Export Job as .txt" so formatting rules
/// (like collapsing a section's sole redundant question label) are decided in one place.
/// </summary>
public interface ISpecFormattingService
{
    /// <summary>Scalar: non-blank after trim. Checkbox: true only if explicitly checked (unset and explicitly-false are both "unanswered"). List: at least one item with at least one non-blank field.</summary>
    bool IsAnswered(QuestionDef question, Room room);

    IReadOnlyList<RoomSpecSection> GetRoomSpecSections(Room room, QuestionSet questionSet);

    string BuildRoomText(Room room, QuestionSet questionSet);

    /// <summary>Whole-job .txt export: job header fields followed by each named room's BuildRoomText.</summary>
    string BuildJobText(Job job, QuestionSet questionSet);
}

public sealed record RoomSpecSection(string Title, IReadOnlyList<RoomSpecLine> Lines)
{
    /// <summary>Shown in place of sections for a room with nothing answered.</summary>
    public const string NoDetailsText = "No details entered for this room.";
}

/// <summary>Either a single value line (IsList == false, Value set) or a list-question's rendered rows (IsList == true, ListLines set).</summary>
public sealed record RoomSpecLine(bool IsList, string Label, bool ShowLabel, string? Value, IReadOnlyList<string>? ListLines)
{
    /// <summary>
    /// The text lines this entry renders as, shared by text export and print so they word things the same way:
    /// a value is "Label: Value" (or just the value when ShowLabel is false); a list is an optional "Label:"
    /// line followed by its rows, flagged IsListItem so each renderer can indent them its own way.
    /// </summary>
    public IEnumerable<(string Text, bool IsListItem)> ToTextLines()
    {
        if (!IsList)
        {
            yield return (ShowLabel ? $"{Label}: {Value}" : Value ?? "", false);
            yield break;
        }

        if (ShowLabel)
        {
            yield return ($"{Label}:", false);
        }
        foreach (var listLine in ListLines ?? [])
        {
            yield return (listLine, true);
        }
    }
}
