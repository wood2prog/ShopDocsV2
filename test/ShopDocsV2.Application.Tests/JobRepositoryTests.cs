using Microsoft.Data.Sqlite;
using ShopDocsV2.Domain;
using ShopDocsV2.Infrastructure.Sqlite;
using Xunit;

namespace ShopDocsV2.Application.Tests;

public class JobRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"shopdocsv2-jobrepo-test-{Guid.NewGuid():N}.db");
    private readonly JobRepository _repository;

    public JobRepositoryTests()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        new SchemaInitializer(factory).Initialize();
        _repository = new JobRepository(factory);
    }

    [Fact]
    public async Task CreateThenGet_RoundTripsJobAndRoomsAcrossANewConnection()
    {
        var job = new Job
        {
            CustomerName = "Jane Smith",
            CustomerPhone = "(555) 123-4567",
            CustomerEmail = "jane@example.com",
            Address = "123 Main St",
            DateCreated = new DateTime(2026, 1, 5),
            DueDate = new DateTime(2026, 3, 1),
            Rooms =
            {
                new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 0 },
                new Room { Id = Guid.NewGuid(), Name = "Pantry", SortOrder = 1 }
            }
        };

        var id = await _repository.CreateJobAsync(job);

        // Simulates "close and reopen the app": a brand-new repository instance reading from the same file.
        var reopened = new JobRepository(new SqliteConnectionFactory(_dbPath));
        var reloaded = await reopened.GetJobAsync(id);

        Assert.NotNull(reloaded);
        Assert.Equal("Jane Smith", reloaded.CustomerName);
        Assert.Equal("(555) 123-4567", reloaded.CustomerPhone);
        Assert.Equal("jane@example.com", reloaded.CustomerEmail);
        Assert.Equal("123 Main St", reloaded.Address);
        Assert.Equal(new DateTime(2026, 1, 5), reloaded.DateCreated);
        Assert.Equal(new DateTime(2026, 3, 1), reloaded.DueDate);
        Assert.Equal(["Kitchen", "Pantry"], reloaded.Rooms.OrderBy(r => r.SortOrder).Select(r => r.Name));
    }

    [Fact]
    public async Task SaveJobAsync_ReplacesRoomsWithCurrentInMemorySet()
    {
        var job = new Job { CustomerName = "Bob", DateCreated = DateTime.Today };
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 0 });
        var id = await _repository.CreateJobAsync(job);

        job.CustomerName = "Bob Jones";
        job.Rooms.Clear();
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Bath", SortOrder = 0 });
        await _repository.SaveJobAsync(job);

        var reloaded = await _repository.GetJobAsync(id);
        Assert.NotNull(reloaded);
        Assert.Equal("Bob Jones", reloaded.CustomerName);
        Assert.Equal(["Bath"], reloaded.Rooms.Select(r => r.Name));
    }

    [Fact]
    public async Task ListJobsAsync_ReturnsMostRecentlyUpdatedFirst()
    {
        var older = new Job { CustomerName = "Older Job", DateCreated = DateTime.Today };
        await _repository.CreateJobAsync(older);
        await Task.Delay(10);
        var newer = new Job { CustomerName = "Newer Job", DateCreated = DateTime.Today };
        await _repository.CreateJobAsync(newer);

        var summaries = await _repository.ListJobsAsync();

        Assert.Equal(2, summaries.Count);
        Assert.Equal("Newer Job", summaries[0].CustomerName);
        Assert.Equal("Older Job", summaries[1].CustomerName);
    }

    [Fact]
    public async Task DuplicateJobAsync_ClonesRoomsWithNewIds()
    {
        var job = new Job { CustomerName = "Original", DateCreated = DateTime.Today };
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 0 });
        var sourceId = await _repository.CreateJobAsync(job);

        var newId = await _repository.DuplicateJobAsync(sourceId);
        var duplicate = await _repository.GetJobAsync(newId);

        Assert.NotEqual(sourceId, newId);
        Assert.NotNull(duplicate);
        Assert.Equal("Original", duplicate.CustomerName);
        Assert.Single(duplicate.Rooms);
        Assert.Equal("Kitchen", duplicate.Rooms[0].Name);
        Assert.NotEqual(job.Rooms[0].Id, duplicate.Rooms[0].Id);

        var original = await _repository.GetJobAsync(sourceId);
        Assert.NotNull(original);
    }

    [Fact]
    public async Task DeleteJobAsync_RemovesJobAndItsRooms()
    {
        var job = new Job { CustomerName = "Temp", DateCreated = DateTime.Today };
        job.Rooms.Add(new Room { Id = Guid.NewGuid(), Name = "Kitchen", SortOrder = 0 });
        var id = await _repository.CreateJobAsync(job);

        await _repository.DeleteJobAsync(id);

        Assert.Null(await _repository.GetJobAsync(id));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
