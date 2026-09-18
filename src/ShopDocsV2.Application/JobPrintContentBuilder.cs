using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public sealed class JobPrintContentBuilder : IJobPrintContentBuilder
{
    private readonly ISpecFormattingService _specFormatting;

    public JobPrintContentBuilder(ISpecFormattingService specFormatting)
    {
        _specFormatting = specFormatting;
    }

    public PrintableJobSpec Build(Job job, QuestionSet questionSet)
    {
        var headerFields = new (string Label, string? Value)[]
        {
            ("Customer", job.CustomerName),
            ("Phone", job.CustomerPhone),
            ("Email", job.CustomerEmail),
            ("Address", job.Address),
            ("Date Created", job.DateCreated.ToString("yyyy-MM-dd")),
            ("Due Date", job.DueDate?.ToString("yyyy-MM-dd"))
        }
        .Where(f => !string.IsNullOrWhiteSpace(f.Value))
        .Select(f => (f.Label, f.Value!))
        .ToList();

        var rooms = job.Rooms
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .OrderBy(r => r.SortOrder)
            .Select(r => (r.Name, _specFormatting.GetRoomSpecSections(r, questionSet)))
            .ToList();

        return new PrintableJobSpec(headerFields, rooms);
    }
}
