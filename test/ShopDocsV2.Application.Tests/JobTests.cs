using ShopDocsV2.Domain;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class JobTests
{
    private static Job NewBlankJob() => new() { Id = Guid.NewGuid(), DateCreated = DateTime.Today, UpdatedAt = DateTime.Now };

    [Fact]
    public void HasContent_BrandNewJob_IsFalse()
    {
        Assert.False(NewBlankJob().HasContent);
    }

    [Fact]
    public void HasContent_WhitespaceOnlyFields_IsFalse()
    {
        var job = NewBlankJob();
        job.CustomerName = "   ";
        job.CustomerPhone = "";
        job.Address = " ";
        Assert.False(job.HasContent);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("email")]
    [InlineData("address")]
    public void HasContent_AnyCustomerField_IsTrue(string field)
    {
        var job = NewBlankJob();
        switch (field)
        {
            case "name": job.CustomerName = "Smith"; break;
            case "phone": job.CustomerPhone = "(555) 123-4567"; break;
            case "email": job.CustomerEmail = "a@b.com"; break;
            case "address": job.Address = "1 Main St"; break;
        }
        Assert.True(job.HasContent);
    }

    [Fact]
    public void HasContent_DueDateSet_IsTrue()
    {
        var job = NewBlankJob();
        job.DueDate = DateTime.Today.AddDays(14);
        Assert.True(job.HasContent);
    }

    [Fact]
    public void HasContent_AnyRoom_IsTrue()
    {
        var job = NewBlankJob();
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Kitchen" });
        Assert.True(job.HasContent);
    }

    [Fact]
    public void NamedRooms_SkipsBlankNamesAndOrdersBySortOrder()
    {
        var job = NewBlankJob();
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Pantry", SortOrder = 2 });
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "  ", SortOrder = 0 });
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 1 });

        Assert.Equal(new[] { "Kitchen", "Pantry" }, job.NamedRooms.Select(r => r.Name));
    }

    [Fact]
    public void Duplicate_CopiesGraphUnderNewIdsWithoutSharingCollections()
    {
        var item = new RoomListItem { Id = Guid.NewGuid(), SortOrder = 3 };
        item.Fields["wood"] = AnswerValue.Of("Maple");
        var room = new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 1 };
        room.Answers["soft_close"] = AnswerValue.Of(true);
        room.ListAnswers["cabinet_finishes"] = [item];
        var job = new Job
        {
            Id = Guid.NewGuid(), CustomerName = "Smith", DueDate = new DateTime(2026, 10, 1),
            DateCreated = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 2), Rooms = [room]
        };
        var now = new DateTime(2026, 9, 24, 8, 30, 0);

        var copy = job.Duplicate(now);

        Assert.NotEqual(job.Id, copy.Id);
        Assert.Equal("Smith", copy.CustomerName);
        Assert.Equal(job.DueDate, copy.DueDate);
        Assert.Equal(now, copy.DateCreated);
        Assert.Equal(now, copy.UpdatedAt);

        var copiedRoom = Assert.Single(copy.Rooms);
        Assert.NotEqual(room.Id, copiedRoom.Id);
        Assert.Equal(("Kitchen", 1), (copiedRoom.Name, copiedRoom.SortOrder));
        Assert.Equal(AnswerValue.Of(true), copiedRoom.Answers["soft_close"]);

        var copiedItem = Assert.Single(copiedRoom.ListAnswers["cabinet_finishes"]);
        Assert.NotEqual(item.Id, copiedItem.Id);
        Assert.Equal(3, copiedItem.SortOrder);
        Assert.Equal("Maple", copiedItem.Fields["wood"].Text);

        // Editing the copy must not touch the original.
        copiedRoom.Answers.Clear();
        copiedItem.Fields.Clear();
        copiedRoom.ListAnswers["cabinet_finishes"].Clear();
        Assert.Single(room.Answers);
        Assert.Single(item.Fields);
        Assert.Single(room.ListAnswers["cabinet_finishes"]);
    }
}
