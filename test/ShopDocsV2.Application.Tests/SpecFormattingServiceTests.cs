using ShopDocsV2.Domain;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class SpecFormattingServiceTests
{
    private readonly SpecFormattingService _sut = new();
    private readonly QuestionSet _questions = TestFixtures.BuildQuestionSet();

    [Fact]
    public void IsAnswered_Checkbox_UnsetOrExplicitlyFalse_BothCountAsUnanswered()
    {
        var question = _questions.Sections[0].Questions.Single(q => q.Id == "soft_close");

        var untouched = new Room();
        var explicitlyFalse = new Room { Answers = { ["soft_close"] = AnswerValue.Of(false) } };
        var checkedTrue = new Room { Answers = { ["soft_close"] = AnswerValue.Of(true) } };

        Assert.False(_sut.IsAnswered(question, untouched));
        Assert.False(_sut.IsAnswered(question, explicitlyFalse));
        Assert.True(_sut.IsAnswered(question, checkedTrue));
    }

    [Fact]
    public void IsAnswered_List_RequiresAtLeastOneNonBlankFieldOnAnItem()
    {
        var question = _questions.Sections[0].Questions.Single(q => q.Id == "cabinet_finishes");

        var emptyList = new Room { ListAnswers = { ["cabinet_finishes"] = new List<RoomListItem> { new() { Id = Guid.NewGuid() } } } };
        var filled = new Room { ListAnswers = { ["cabinet_finishes"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("wood", "Maple")) } } };

        Assert.False(_sut.IsAnswered(question, emptyList));
        Assert.True(_sut.IsAnswered(question, filled));
    }

    [Fact]
    public void GetRoomSpecSections_CollapsesRedundantLabel_WhenSoleQuestionMatchesSectionTitle()
    {
        // "Countertops" section has exactly one question, also labeled "Countertops" -> label should be dropped.
        var room = new Room
        {
            Name = "Kitchen",
            ListAnswers =
            {
                ["countertops"] = new List<RoomListItem>
                {
                    TestFixtures.ListItem(0, ("material", "Quartz"), ("color", "Calacatta Bali"), ("edge_profile", "Eased"))
                }
            }
        };

        var sections = _sut.GetRoomSpecSections(room, _questions);
        var countertopSection = sections.Single(s => s.Title == "Countertops");
        var line = Assert.Single(countertopSection.Lines);

        Assert.False(line.ShowLabel);
        Assert.True(line.IsList);
        Assert.Equal(new[] { "Quartz — Calacatta Bali — Eased" }, line.ListLines);
    }

    [Fact]
    public void GetRoomSpecSections_CombinesPrintGroupFields_AndSkipsBlankOnes()
    {
        var room = new Room
        {
            ListAnswers =
            {
                ["appliance_package"] = new List<RoomListItem>
                {
                    TestFixtures.ListItem(0, ("name", "Range"), ("model", "RF-30"),
                        ("cutout_width", "30"), ("cutout_height", "36"), ("cutout_depth", "24")),
                    TestFixtures.ListItem(1, ("name", "Dishwasher"), ("cutout_width", "24"), ("cutout_depth", "24")),
                    TestFixtures.ListItem(2, ("name", "Microwave"))
                }
            }
        };

        var line = Assert.Single(_sut.GetRoomSpecSections(room, _questions).Single(s => s.Title == "Appliances").Lines);

        Assert.Equal(new[]
        {
            "Range — RF-30 — Cutout - 30W x 36H x 24D",
            "Dishwasher — Cutout - 24W x 24D",
            "Microwave"
        }, line.ListLines);
    }

    [Fact]
    public void BuildRoomText_OmitsRedundantLabelLine_AndSkipsUnansweredSections()
    {
        var room = new Room
        {
            Name = "Pantry",
            ListAnswers =
            {
                ["countertops"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("material", "Laminate")) }
            }
        };

        var text = _sut.BuildRoomText(room, _questions);

        Assert.Contains("Pantry", text);
        Assert.Contains("Countertops\n  Laminate", text);
        Assert.DoesNotContain("Countertops:", text); // redundant question label collapsed
        Assert.DoesNotContain("Cabinets", text);      // unanswered section skipped entirely
    }

    [Fact]
    public void BuildRoomText_NoDetails_ReturnsPlaceholderLine()
    {
        var text = _sut.BuildRoomText(new Room { Name = "Empty Room" }, _questions);
        Assert.Equal("Empty Room\n\nNo details entered for this room.\n", text);
    }

    [Fact]
    public void BuildJobText_IncludesHeaderAndOnlyNamedRoomsInSortOrder_AndSkipsBlankHeaderFields()
    {
        var job = new Job
        {
            CustomerName = "Jane Smith",
            CustomerPhone = null,
            Address = "123 Main St",
            DateCreated = new DateTime(2026, 1, 5)
        };
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "", SortOrder = 0 });
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

        var text = _sut.BuildJobText(job, _questions);

        Assert.Contains("Customer: Jane Smith", text);
        Assert.Contains("Address: 123 Main St", text);
        Assert.DoesNotContain("Phone:", text);
        Assert.True(text.IndexOf("Kitchen", StringComparison.Ordinal) < text.IndexOf("Pantry", StringComparison.Ordinal));
    }
}
