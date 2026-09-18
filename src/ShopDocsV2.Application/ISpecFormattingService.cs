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

public sealed record RoomSpecSection(string Title, IReadOnlyList<RoomSpecLine> Lines);

/// <summary>Either a single value line (IsList == false, Value set) or a list-question's rendered rows (IsList == true, ListLines set).</summary>
public sealed record RoomSpecLine(bool IsList, string Label, bool ShowLabel, string? Value, IReadOnlyList<string>? ListLines);
