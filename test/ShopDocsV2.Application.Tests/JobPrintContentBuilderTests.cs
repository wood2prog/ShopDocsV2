using ShopDocsV2.Domain;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class JobPrintContentBuilderTests
{
    private readonly JobPrintContentBuilder _sut = new(new SpecFormattingService());
    private readonly QuestionSet _questions = TestFixtures.BuildQuestionSet();

    [Fact]
    public void Build_IncludesOnlyNonBlankHeaderFields()
    {
        var job = new Job
        {
            CustomerName = "Jane Smith",
            CustomerPhone = "(555) 123-4567",
            CustomerEmail = null,
            Address = "",
            DateCreated = new DateTime(2026, 1, 5),
            DueDate = null
        };

        var spec = _sut.Build(job, _questions);

        Assert.Equal(
            new[] { ("Customer", "Jane Smith"), ("Phone", "(555) 123-4567"), ("Date Created", "2026-01-05") },
            spec.HeaderFields.Select(f => (f.Label, f.Value)));
    }

    [Fact]
    public void Build_OnlyIncludesNamedRooms_InSortOrder()
    {
        var job = new Job { CustomerName = "Bob", DateCreated = DateTime.Today };
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "", SortOrder = 0 }); // unnamed: excluded
        job.Rooms.Add(new Room
        {
            Id = Guid.NewGuid(),
            Name = "Pantry",
            SortOrder = 2,
            ListAnswers = { ["countertops"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("material", "Laminate")) } }
        });
        job.Rooms.Add(new Room
        {
            Id = Guid.NewGuid(),
            Name = "Kitchen",
            SortOrder = 1,
            Answers = { ["cabinet_style"] = AnswerValue.Of("Framed") }
        });

        var spec = _sut.Build(job, _questions);

        Assert.Equal(["Kitchen", "Pantry"], spec.Rooms.Select(r => r.RoomName));
        var kitchenSections = spec.Rooms.Single(r => r.RoomName == "Kitchen").Sections;
        Assert.Contains(kitchenSections, s => s.Title == "Cabinets");
    }
}
