using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface IJobPrintContentBuilder
{
    /// <summary>Builds the printable content model for a job: header fields plus each named room's answered specs, in room order.</summary>
    PrintableJobSpec Build(Job job, QuestionSet questionSet);
}

public sealed record PrintableJobSpec(
    IReadOnlyList<(string Label, string Value)> HeaderFields,
    IReadOnlyList<(string RoomName, IReadOnlyList<RoomSpecSection> Sections)> Rooms);
