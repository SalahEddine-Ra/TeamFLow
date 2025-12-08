namespace TeamFlowAPI.Models.Entities;

public class Organization
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Settings { get; set; } = "{}"; // JSON string
    public long? OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public List<OrganizationUser> OrganizationUsers { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public List<ActivityLog> ActivityLogs { get; set; } = new();
    public List<Invitation> Invitations { get; set; } = new();
}