namespace TeamFlowAPI.Models.Entities;

public class Project
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public long? TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public long? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Team? Team { get; set; }
    public User? Creator { get; set; }
    public List<TaskItem> Tasks { get; set; } = new();
}