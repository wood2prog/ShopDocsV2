namespace ShopDocsV2.Domain;

public sealed class Job
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Address { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Room> Rooms { get; set; } = new();

    /// <summary>Rooms that have a name, in tab order. Unnamed rooms are left out of print and text export.</summary>
    public IEnumerable<Room> NamedRooms => Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Name)).OrderBy(r => r.SortOrder);

    /// <summary>
    /// Whether the user has entered anything worth saving: any customer field, a due date, or any room.
    /// DateCreated doesn't count since every new job gets one automatically.
    /// </summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(CustomerName) ||
        !string.IsNullOrWhiteSpace(CustomerPhone) ||
        !string.IsNullOrWhiteSpace(CustomerEmail) ||
        !string.IsNullOrWhiteSpace(Address) ||
        DueDate is not null ||
        Rooms.Count > 0;

    /// <summary>
    /// A copy of this job's whole graph (customer fields, rooms, answers, list items) under new ids, created and
    /// updated at <paramref name="now"/>. Used by Duplicate Job, which replaces the original app's file-based "Save As".
    /// </summary>
    public Job Duplicate(DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        CustomerName = CustomerName,
        CustomerPhone = CustomerPhone,
        CustomerEmail = CustomerEmail,
        Address = Address,
        DateCreated = now,
        DueDate = DueDate,
        UpdatedAt = now,
        Rooms = Rooms.Select(r => r.Duplicate()).ToList()
    };
}
