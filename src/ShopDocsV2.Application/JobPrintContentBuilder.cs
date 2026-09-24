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
        var rooms = job.NamedRooms
            .Select(r => (r.Name, _specFormatting.GetRoomSpecSections(r, questionSet)))
            .ToList();

        return new PrintableJobSpec(JobHeader.Fields(job), rooms);
    }
}
