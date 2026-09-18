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
}
