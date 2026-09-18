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
                        .Select(item => string.Join(" — ", itemFields
                            .Select(f => item.Fields.TryGetValue(f.Id, out var v) && !v.IsBlank ? v.DisplayText : null)
                            .Where(s => s is not null)))
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

    public string BuildRoomText(Room room, QuestionSet questionSet)
    {
        var lines = new List<string> { string.IsNullOrWhiteSpace(room.Name) ? "Room" : room.Name, "" };

        var sections = GetRoomSpecSections(room, questionSet);
        if (sections.Count == 0)
        {
            lines.Add("No details entered for this room.");
            return JoinTrimmed(lines);
        }

        foreach (var section in sections)
        {
            lines.Add(section.Title);
            foreach (var item in section.Lines)
            {
                if (item.IsList)
                {
                    if (item.ShowLabel) lines.Add($"{item.Label}:");
                    foreach (var l in item.ListLines ?? Array.Empty<string>()) lines.Add($"  {l}");
                    continue;
                }
                lines.Add(item.ShowLabel ? $"{item.Label}: {item.Value}" : $"{item.Value}");
            }
            lines.Add("");
        }

        return JoinTrimmed(lines);
    }

    public string BuildJobText(Job job, QuestionSet questionSet)
    {
        var headerFields = new (string Label, string? Value)[]
        {
            ("Customer", job.CustomerName),
            ("Phone", job.CustomerPhone),
            ("Email", job.CustomerEmail),
            ("Address", job.Address),
            ("Date Created", job.DateCreated.ToString("yyyy-MM-dd")),
            ("Due Date", job.DueDate?.ToString("yyyy-MM-dd"))
        }.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Job Specification");
        foreach (var (label, value) in headerFields) sb.AppendLine($"{label}: {value}");
        sb.AppendLine();

        var namedRooms = job.Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Name)).OrderBy(r => r.SortOrder).ToList();
        foreach (var room in namedRooms)
        {
            sb.Append(BuildRoomText(room, questionSet));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string JoinTrimmed(IEnumerable<string> lines) => string.Join("\n", lines).Trim() + "\n";
}
