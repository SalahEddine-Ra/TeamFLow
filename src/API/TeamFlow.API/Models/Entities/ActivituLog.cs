namespace TeamFlowAPI.Models.Entities;
public class ActivityLog
{
    public long Id { get; set; }
    public long? OrgId { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Payload { get; set; } // JSON string
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Organization? Organization { get; set; }
    public User? User { get; set; }
}