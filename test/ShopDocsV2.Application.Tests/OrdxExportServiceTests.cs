using System.Xml.Linq;
using ShopDocsV2.Domain;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class OrdxExportServiceTests
{
    private readonly OrdxExportService _sut = new();
    private readonly QuestionSet _questions = TestFixtures.BuildQuestionSet();

    private static Room KitchenRoom() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Kitchen",
        SortOrder = 0,
        Answers =
        {
            ["cabinet_style"] = AnswerValue.Of("Frameless"),
            ["cabinet_door_style"] = AnswerValue.Of("Shaker"),
            ["soft_close"] = AnswerValue.Of(true)
        },
        ListAnswers =
        {
            ["cabinet_finishes"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("name", "Perimeter"), ("wood", "Maple"), ("finish", "OCS White")) },
            ["cabinet_hardware"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("name", "Pull"), ("catalogItem", "Chrome4inC-Pull"), ("color", "Chrome")) }
        }
    };

    [Fact]
    public void BuildOrdxXml_SingleAnsweredRoom_ReflectsItsAnswersAsJobDefaults()
    {
        var job = new Job { CustomerName = "Jane Doe", Rooms = { KitchenRoom() } };

        var xml = _sut.BuildOrdxXml(job, _questions, new DateTime(2026, 1, 15, 14, 30, 5));

        Assert.Contains("<ProductVersion>2026</ProductVersion>", xml);
        Assert.Contains("<Job Created=\"1/15/2026 2:30:05 PM\">", xml);
        Assert.Contains("<Name>Jane Doe</Name>", xml);
    }

    [Fact]
    public void BuildOrdxXml_FramelessAndSoftClose_MapToExpectedConstructionAndHardware()
    {
        var job = new Job { CustomerName = "Jane Doe", Rooms = { KitchenRoom() } };

        var xml = _sut.BuildOrdxXml(job, _questions);

        Assert.Contains("<Cabinet>32mm</Cabinet>", xml);
        Assert.Contains("<DrawerBox>OCS Standard Softclose Drawer</DrawerBox>", xml);
        Assert.Contains("<PullSchedule>Chrome4inC-Pull</PullSchedule>", xml);
        Assert.Contains("<Style>Shaker</Style>", xml);
        Assert.Contains("<Name>Kitchen</Name>", xml);
        Assert.Contains("OCS Maple Doors", xml);
    }

    [Fact]
    public void BuildOrdxXml_NoRoomHasAnyAnswer_StillEmitsPropertiesWithGenericDefaults()
    {
        var job = new Job { CustomerName = "Jane Doe", Rooms = { new Room { Id = Guid.NewGuid(), Name = "Kitchen" } } };

        var xml = _sut.BuildOrdxXml(job, _questions);

        Assert.Contains("<PullSchedule>OCS Standard Pulls</PullSchedule>", xml);
        Assert.Contains("<DrawerBox>OCS Standard Drawer</DrawerBox>", xml); // non-soft-close default
        Assert.Contains("<Name>Kitchen</Name>", xml); // named room still gets a <Room> block even with no answers
    }

    [Fact]
    public void BuildOrdxXml_SecondRoomWithDivergentFinish_GetsOverrideBlocks_PrimaryRoomDoesNot()
    {
        var kitchen = KitchenRoom();
        var pantry = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Pantry",
            SortOrder = 1,
            ListAnswers =
            {
                ["cabinet_finishes"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("wood", "Cherry"), ("finish", "OCS Natural Cherry")) }
            }
        };
        var job = new Job { CustomerName = "Jane Doe", Rooms = { kitchen, pantry } };

        var xml = _sut.BuildOrdxXml(job, _questions);

        // Primary room (Kitchen, first with any answer) gets an empty room-level Cabinet override.
        var kitchenRoomBlock = ExtractRoomBlock(xml, "Kitchen");
        Assert.Contains("<Cabinet>\n        </Cabinet>", Normalize(kitchenRoomBlock));

        // Pantry diverges in wood -> gets a real Materials + Doors override, and a Molding override.
        var pantryRoomBlock = ExtractRoomBlock(xml, "Pantry");
        Assert.Contains("OCS Cherry/Prefinished", pantryRoomBlock);
        Assert.Contains("<Material>Cherry</Material>", pantryRoomBlock);
    }

    [Fact]
    public void BuildOrdxXml_ExtraListAnswers_AreCapturedInXmlComment()
    {
        var kitchen = KitchenRoom();
        kitchen.ListAnswers["countertops"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("material", "Quartz"), ("color", "Calacatta Bali")) };
        var job = new Job { CustomerName = "Jane Doe", Rooms = { kitchen } };

        var xml = _sut.BuildOrdxXml(job, _questions);

        Assert.Contains("Countertop: Quartz / Calacatta Bali", xml);
        Assert.Contains("Hardware: Pull / Chrome4inC-Pull / Chrome", xml);
    }

    [Fact]
    public void BuildOrdxXml_MultiRoomDivergentFinishExport_IsWellFormedXml()
    {
        var kitchen = KitchenRoom();
        var pantry = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Pantry",
            SortOrder = 1,
            ListAnswers =
            {
                ["cabinet_finishes"] = new List<RoomListItem> { TestFixtures.ListItem(0, ("wood", "Cherry"), ("finish", "OCS Natural Cherry")) }
            }
        };
        var job = new Job { CustomerName = "Jane Doe", Rooms = { kitchen, pantry } };

        var xml = _sut.BuildOrdxXml(job, _questions);

        var doc = XDocument.Parse(xml); // throws on malformed XML
        Assert.Equal("Job", doc.Root!.Name.LocalName);
        Assert.Equal(2, doc.Root.Descendants("Rooms").Single().Elements("Room").Count());
    }

    /// <summary>
    /// Extracts a whole top-level &lt;Room&gt;...&lt;/Room&gt; block (from &lt;Rooms&gt;, not the
    /// job-level Properties/Room) by name, via balanced-tag scanning since RoomProperties nests
    /// another &lt;Room&gt;...&lt;/Room&gt; one level deep.
    /// </summary>
    private static string ExtractRoomBlock(string xml, string roomName)
    {
        var roomsStart = xml.IndexOf("<Rooms>", StringComparison.Ordinal);
        Assert.True(roomsStart >= 0, "<Rooms> section not found");
        var marker = $"<Name>{roomName}</Name>";
        var markerIndex = xml.IndexOf(marker, roomsStart, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Room '{roomName}' not found in <Rooms>");
        var outerStart = xml.LastIndexOf("<Perspective>", markerIndex, StringComparison.Ordinal);
        outerStart = xml.LastIndexOf("<Room>", outerStart, StringComparison.Ordinal);

        int depth = 0;
        int cursor = outerStart;
        while (true)
        {
            var nextOpen = xml.IndexOf("<Room>", cursor, StringComparison.Ordinal);
            var nextClose = xml.IndexOf("</Room>", cursor, StringComparison.Ordinal);
            if (nextOpen >= 0 && nextOpen < nextClose)
            {
                depth++;
                cursor = nextOpen + "<Room>".Length;
            }
            else
            {
                depth--;
                cursor = nextClose + "</Room>".Length;
                if (depth == 0) return xml[outerStart..cursor];
            }
        }
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");
}
