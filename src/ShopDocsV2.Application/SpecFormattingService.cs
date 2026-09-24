using System.Text;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public sealed class SpecFormattingService : ISpecFormattingService
{
    public bool IsAnswered(QuestionDef question, Room room)
    {
        if (question.Type == QuestionType.Checkbox)
        {
            return room.Answers.TryGetValue(question.Id, out var cb) && cb.Kind == AnswerValueKind.Bool && (cb.Bool ?? false);
        }

        if (question.Type == QuestionType.List)
        {
            if (!room.ListAnswers.TryGetValue(question.Id, out var items) || items.Count == 0) return false;
            var itemFields = question.ItemFields ?? new List<QuestionDef>();
            return items.Any(item => itemFields.Any(f => item.Fields.TryGetValue(f.Id, out var v) && !v.IsBlank));
        }

        return room.Answers.TryGetValue(question.Id, out var val) && !val.IsBlank;
    }

    public IReadOnlyList<RoomSpecSection> GetRoomSpecSections(Room room, QuestionSet questionSet)
    {
        var sections = new List<RoomSpecSection>();

        foreach (var section in questionSet.Sections)
        {
            var answeredQs = section.Questions.Where(q => IsAnswered(q, room)).ToList();
            if (answeredQs.Count == 0) continue;

            bool skipRedundantLabel = answeredQs.Count == 1
                && string.Equals(answeredQs[0].Label.Trim(), section.Title.Trim(), StringComparison.OrdinalIgnoreCase);

            var lines = new List<RoomSpecLine>();
            foreach (var q in answeredQs)
            {
                bool showLabel = !skipRedundantLabel;

                if (q.Type == QuestionType.List)
                {
                    var itemFields = q.ItemFields ?? new List<QuestionDef>();
                    var items = room.ListAnswers.TryGetValue(q.Id, out var list) ? list : new List<RoomListItem>();
                    var listLines = items
                        .OrderBy(i => i.SortOrder)
                        .Select(item => FormatListItem(item, itemFields))
                        .Where(line => line != "")
                        .ToList();
                    lines.Add(new RoomSpecLine(IsList: true, q.Label, showLabel, Value: null, ListLines: listLines));
                }
                else
                {
                    string value = q.Type == QuestionType.Checkbox
                        ? "Yes"
                        : (room.Answers.TryGetValue(q.Id, out var v) ? v.DisplayText : "");
                    lines.Add(new RoomSpecLine(IsList: false, q.Label, showLabel, Value: value, ListLines: null));
                }
            }

            sections.Add(new RoomSpecSection(section.Title, lines));
        }

        return sections;
    }

    /// <summary>
    /// Joins an item's non-blank fields with " — ". Adjacent fields sharing a PrintGroup collapse into one
    /// part, e.g. "Cutout - 30W x 36H x 24D", keeping only the ones filled in.
    /// </summary>
    private static string FormatListItem(RoomListItem item, List<QuestionDef> itemFields)
    {
        string? ValueOf(QuestionDef f) => item.Fields.TryGetValue(f.Id, out var v) && !v.IsBlank ? v.DisplayText : null;

        var parts = new List<string>();
        for (var i = 0; i < itemFields.Count; i++)
        {
            var group = itemFields[i].PrintGroup;
            if (string.IsNullOrEmpty(group))
            {
                if (ValueOf(itemFields[i]) is { } value) parts.Add(value);
                continue;
            }

            var groupValues = new List<string>();
            for (; i < itemFields.Count && itemFields[i].PrintGroup == group; i++)
            {
                if (ValueOf(itemFields[i]) is { } value) groupValues.Add(value + itemFields[i].PrintSuffix);
            }
            i--;

            if (groupValues.Count > 0) parts.Add($"{group} - {string.Join(" x ", groupValues)}");
        }

        return string.Join(" — ", parts);
    }

    public string BuildRoomText(Room room, QuestionSet questionSet)
    {
        var lines = new List<string> { string.IsNullOrWhiteSpace(room.Name) ? "Room" : room.Name, "" };

        var sections = GetRoomSpecSections(room, questionSet);
        if (sections.Count == 0)
        {
            lines.Add(RoomSpecSection.NoDetailsText);
            return JoinTrimmed(lines);
        }

        foreach (var section in sections)
        {
            lines.Add(section.Title);
            foreach (var (text, isListItem) in section.Lines.SelectMany(l => l.ToTextLines()))
            {
                lines.Add(isListItem ? $"  {text}" : text);
            }
            lines.Add("");
        }

        return JoinTrimmed(lines);
    }

    public string BuildJobText(Job job, QuestionSet questionSet)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Job Specification");
        foreach (var (label, value) in JobHeader.Fields(job)) sb.AppendLine($"{label}: {value}");
        sb.AppendLine();

        foreach (var room in job.NamedRooms)
        {
            sb.Append(BuildRoomText(room, questionSet));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string JoinTrimmed(IEnumerable<string> lines) => string.Join("\n", lines).Trim() + "\n";
}
