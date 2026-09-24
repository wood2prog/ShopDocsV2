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
}
