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
}
